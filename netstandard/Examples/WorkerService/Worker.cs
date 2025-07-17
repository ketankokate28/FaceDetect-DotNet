using FaceONNX;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WorkerService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IConfiguration _configuration;
        string connStr = string.Empty;
        private readonly AppPathsOptions _paths;
        private readonly AppSettingsOptions _appSettings;
        private Dictionary<string, float[]> cachedSuspectEmbeddings = null;
        ConcurrentQueue<string> imageQueue = new ConcurrentQueue<string>();
        private FileSystemWatcher watcher1;
        private ProcessingWorker[] workers;
        private int workerIndex = 0;
        private readonly object workerLock = new object();
        private readonly object _embeddingLock = new();
        private DateTime _lastLoadedTime = DateTime.MinValue;
        private readonly TaskCompletionSource<bool> _initialLoadCompleted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public record SuspectMeta(int Id, string Name, DateTime UpdatedAt, List<string> ImageUrls);
        string lastSyncTimestr = null;
        public class MetadataResponse
        {
            public string last_sync_time { get; set; }
            public List<SuspectMeta> Suspects { get; set; }
        }

        public Worker(ILogger<Worker> logger, IConfiguration configuration, IOptions<AppPathsOptions> paths,
             IOptions<AppSettingsOptions> appSettings)
        {
            _logger = logger;
            _configuration = configuration;
            _appSettings = appSettings.Value;
            connStr = _configuration.GetConnectionString("DefaultConnection");
            _paths = paths.Value;
           // PrecomputeSuspectEmbeddingsAsync(Path.Combine(AppContext.BaseDirectory, "suspects"));
        }
        private void EnsureDirectoriesExist()
        {
            CreateIfNotExists(_paths.ResultDir);
            CreateIfNotExists(_paths.TempResultDir);
            CreateIfNotExists(_paths.FramesDir);
            CreateIfNotExists(_paths.SuspectDir);
        }
        private void CreateIfNotExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                _logger.LogInformation("Created missing directory: {0}", path);
            }
            else
            {
                _logger.LogInformation("Directory already exists: {0}", path);
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            EnsureDirectoriesExist();

            // Start suspect reload loop (periodic)
            _ = Task.Run(() => ReloadSuspectsLoop(stoppingToken));

            // Wait for initial suspect load before proceeding
            await _initialLoadCompleted.Task;

            // Start main logic (continuous)
            await Process(stoppingToken);
        }
        private async Task Process(CancellationToken stoppingToken)
        {
            bool workersInitialized = false;

            //while (!stoppingToken.IsCancellationRequested)
            //{
                Dictionary<string, float[]> embeddingsSnapshot;
                    _logger.LogInformation("Starting processing workers...");

                    if (!Directory.Exists(_paths.TempResultDir))
                    {
                        Directory.CreateDirectory(_paths.TempResultDir);
                    }
                    else
                    {
                        Array.ForEach(Directory.GetFiles(_paths.TempResultDir), File.Delete);
                    }

                    var allImageFiles = Directory.GetFiles(_paths.FramesDir)
                        .Where(f => IsImage(f))
                        .ToArray();

                    //foreach (var img in allImageFiles)
                    //{
                    //    imageQueue.Enqueue(img);
                    //}

                 
                    var sharedDetector = new FaceDetector();
                    var sharedEmbedder = new FaceEmbedder();

                  
                workers = Enumerable.Range(0, 4)
.Select(_ => new ProcessingWorker(cachedSuspectEmbeddings, _paths.ResultDir, _paths.TempResultDir, message =>
{
    _logger.LogInformation(message); // enable actual log
}, stoppingToken, sharedDetector, sharedEmbedder, _configuration, _appSettings))
.ToArray();

                foreach (var worker in workers)
                {
                    worker.Start();
                }
                //EnqueueSuspect(cachedSuspectEmbeddings);

                workersInitialized = true;
                    foreach (var img in allImageFiles)
                    {
                        EnqueueImage(img);
                    }
                StartImageWatcher(_paths.FramesDir);

                // Wait a little before checking again
               // await Task.Delay(1000, stoppingToken);
           // }
        }
        private async Task ReloadSuspectsLoop(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Reloading suspects from DB...");

                   // var updatedSuspects = new Dictionary<int, (string name, List<byte[]> blobs)>();

                    var updatedSuspects = new Dictionary<int, (string name, List<string> filePaths)>();

                    var metadata = await FetchSuspectMetadataAsync(_lastLoadedTime, stoppingToken);
                    if (!metadata.Any())
                    {
                        _logger.LogInformation("No updated suspects found.");
                        await Task.Delay(TimeSpan.FromMinutes(_appSettings.SuspectReloadIntervalMinutes), stoppingToken);
                        continue;
                    }

                    // queue missing files
                    var downloadQueue = new ConcurrentQueue<int>();
                    PrepareDownloadQueue(metadata, downloadQueue);

                    // start downloader
                    var downloader = Task.Run(() => DownloadWorkerAsync(downloadQueue, stoppingToken));

                    // wait for downloads
                    while (!downloadQueue.IsEmpty)
                    {
                        await Task.Delay(500, stoppingToken);
                    }

                    // build updated suspects for embedding
                    foreach (var s in metadata)
                    {
                        var suspectFolder = Path.Combine(_paths.SuspectDir, s.Id.ToString());
                        var files = Directory.GetFiles(suspectFolder).ToList();

                        updatedSuspects[s.Id] = (s.Name, files);
                        if (s.UpdatedAt > _lastLoadedTime)
                            _lastLoadedTime = s.UpdatedAt;
                    }
                    if (updatedSuspects.Count > 0)
                    {
                        var matcher = new FaceMatcher(_configuration, _appSettings);
                        var newEmbeddings = await Task.Run(() =>
                            matcher.PrecomputeSuspectEmbeddingsFromFiles(updatedSuspects, msg => _logger.LogInformation(msg)));

                        lock (_embeddingLock)
                        {
                            if (cachedSuspectEmbeddings == null)
                                cachedSuspectEmbeddings = new Dictionary<string, float[]>();

                            foreach (var key in newEmbeddings.Keys)
                            {
                                string prefix = key.Split('-')[0] + "-";
                                foreach (var oldKey in cachedSuspectEmbeddings.Keys.Where(k => k.StartsWith(prefix)).ToList())
                                    cachedSuspectEmbeddings.Remove(oldKey);

                                cachedSuspectEmbeddings[key] = newEmbeddings[key];
                            }

                            EnqueueSuspect(cachedSuspectEmbeddings);
                        }

                        _logger.LogInformation("Reloaded {0} suspects at {1}", newEmbeddings.Count, DateTime.Now);
                        if (!_initialLoadCompleted.Task.IsCompleted)
                        {
                            _initialLoadCompleted.SetResult(true);
                        }
                    }

                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in ReloadSuspectsLoop");
                }

                await Task.Delay(TimeSpan.FromMinutes(_appSettings.SuspectReloadIntervalMinutes), stoppingToken);
            }
        }

        void StartImageWatcher(string imageDir)
        {
            watcher1 = new FileSystemWatcher(imageDir);
            watcher1.IncludeSubdirectories = false;
            watcher1.Filter = "*.*";
            watcher1.NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime;

            watcher1.Created += (s, e) =>
            {
                if (IsImage(e.FullPath))
                {
                    EnqueueImage(e.FullPath);
                }
            };

            watcher1.EnableRaisingEvents = true;
        }
        private void EnqueueImage(string imagePath)
        {
            if (workers == null || workers.Length == 0)
                return; // workers not initialized

            lock (workerLock)
            {
                workers[workerIndex].EnqueueImage(imagePath);
                workerIndex = (workerIndex + 1) % workers.Length;
            }
        }
        private void EnqueueSuspect(Dictionary<string, float[]> _cachedSuspectEmbeddings)
        {
            if (workers == null || workers.Length == 0)
                return; // workers not initialized

            foreach(var worker in workers)
            {
                worker.EnqueueSuspect(_cachedSuspectEmbeddings);
            }
        }
        bool IsImage(string path)
        {
            string ext = Path.GetExtension(path).ToLower();
            return ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".bmp";
        }
        private Dictionary<int, string> LoadSuspectList()
        {
            var suspects = new Dictionary<int, string>();

            try
            {
                using (var connection = new SqliteConnection(connStr))
                {
                    connection.Open();

                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = @"
                    SELECT suspect_id, first_name 
                    FROM suspects;
                ";

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var id = reader.GetInt32(0);              // "id" column
                                var name = reader.IsDBNull(1) ? "" : reader.GetString(1); // "firstName" column, null-safe

                                // Avoid duplicates, or log if needed
                                if (!suspects.ContainsKey(id))
                                {
                                    suspects.Add(id, name);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle or log the exception as per your application's logging strategy
                Console.Error.WriteLine($"Error loading suspect list: {ex.Message}");
                throw; // Optional: rethrow or return empty dictionary
            }

            return suspects;
        }


        private async Task ReloadSuspectsLoop_backup(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Reloading suspects and embeddings...");

                    var suspects = LoadSuspectList();
                    if (suspects.Count > 0)
                    {
                        // Run embedding computation in background
                        var newEmbeddings = await Task.Run(() =>
                        {
                            var matcher = new FaceMatcher(_configuration, _appSettings);

                            var validFolders = Directory.GetDirectories(_paths.SuspectDir)
                                .Where(folderPath =>
                                {
                                    var folderName = Path.GetFileName(folderPath);
                                    return int.TryParse(folderName, out int id) && suspects.ContainsKey(id);
                                })
                                .ToList();

                            return matcher.PrecomputeSuspectEmbeddings(validFolders, message => { });
                        });

                        // Safely replace the shared embeddings
                        lock (_embeddingLock)
                        {
                            cachedSuspectEmbeddings = newEmbeddings;
                        }

                        _logger.LogInformation("Reloaded {0} suspects and embeddings at {1}", suspects.Count, DateTime.Now);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error reloading suspects/embeddings");
                }

                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(_appSettings.SuspectReloadIntervalMinutes), stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    break; // service is shutting down
                }
            }
        }
        private async Task<List<SuspectMeta>> FetchSuspectMetadataAsync(DateTime? lastSyncTime, CancellationToken token)
        {
            string apiUrl = _appSettings.APIURL;
            string fullUrl = apiUrl + "/suspect/metadata";

            if (lastSyncTimestr!= null)
            {
                var query = $"?lastSync={lastSyncTimestr}";
                fullUrl += query;
            }   

            using var client = new HttpClient();
            var response = await client.GetAsync(fullUrl, token);

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(token);
            //var result = System.Text.Json.JsonSerializer.Deserialize<List<SuspectMeta>>(json);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var result = JsonSerializer.Deserialize<MetadataResponse>(json, options);
            lastSyncTimestr = result.last_sync_time;
            return result.Suspects ?? new List<SuspectMeta>();
        }

        private void PrepareDownloadQueue(List<SuspectMeta> updatedSuspects, ConcurrentQueue<int> queue)
        {
            foreach (var suspect in updatedSuspects)
            {
                string suspectFolder = Path.Combine(_paths.SuspectDir, suspect.Id.ToString());
                bool needsUpdate = false;

                foreach (var url in suspect.ImageUrls)
                {
                    var fileName = Path.GetFileName(url);
                    var localPath = Path.Combine(suspectFolder, fileName);

                    if (!File.Exists(localPath))
                    {
                        needsUpdate = true;
                        break;
                    }
                }

                if (needsUpdate)
                {
                    queue.Enqueue(suspect.Id);
                }
            }
        }

        private async Task DownloadWorkerAsync(ConcurrentQueue<int> queue, CancellationToken token)
        {
            using var client = new HttpClient();

            var apiUrl = _appSettings.APIURL.TrimEnd('/');
            var downloadEndpoint = $"{apiUrl}/suspect/images";

            while (!token.IsCancellationRequested)
            {
                if (!queue.TryDequeue(out var suspectId))
                {
                    await Task.Delay(500, token);
                    continue;
                }

                try
                {
                    string suspectFolder = Path.Combine(_paths.SuspectDir, suspectId.ToString());

                    // Clean up old folder
                    if (Directory.Exists(suspectFolder))
                        Directory.Delete(suspectFolder, recursive: true);

                    Directory.CreateDirectory(suspectFolder);

                    var requestBody = new { suspect_id = suspectId };

                    var jsonContent = new StringContent(
                        System.Text.Json.JsonSerializer.Serialize(requestBody),
                        Encoding.UTF8,
                        "application/json"
                    );

                    var response = await client.PostAsync(downloadEndpoint, jsonContent, token);
                    response.EnsureSuccessStatusCode();

                    var responseJson = await response.Content.ReadAsStringAsync(token);
                    var result = System.Text.Json.JsonSerializer.Deserialize<DownloadResponse>(responseJson);

                    if (result?.Images != null)
                    {
                        foreach (var image in result.Images)
                        {
                            if (string.IsNullOrEmpty(image.Base64) || string.IsNullOrEmpty(image.ImagePath))
                                continue;

                            var fileName = Path.GetFileName(image.ImagePath);
                            var localPath = Path.Combine(suspectFolder, fileName);

                            var imageBytes = Convert.FromBase64String(image.Base64);
                            await File.WriteAllBytesAsync(localPath, imageBytes, token);

                            _logger.LogInformation($"Downloaded {image.ImagePath} → {localPath}");
                        }
                    }
                    else
                    {
                        _logger.LogWarning($"No images returned for suspect {suspectId}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to download images for suspect {suspectId}");
                }
            }
        }


        // Define a helper DTO
        public class DownloadResponse
        {
            [JsonPropertyName("images")]
            public List<ImageResult> Images { get; set; }

            public class ImageResult
            {
                [JsonPropertyName("image_path")]
                public string ImagePath { get; set; }

                [JsonPropertyName("base64")]
                public string Base64 { get; set; }
            }
        }

    }
}
