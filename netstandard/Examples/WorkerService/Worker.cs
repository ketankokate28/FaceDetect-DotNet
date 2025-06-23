using FaceONNX;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;

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

            // Start main logic (continuous)
            await Process(stoppingToken);
        }
        private async Task Process(CancellationToken stoppingToken)
        {
            bool workersInitialized = false;

            while (!stoppingToken.IsCancellationRequested)
            {
                Dictionary<string, float[]> embeddingsSnapshot;

                lock (_embeddingLock)
                {
                    if (cachedSuspectEmbeddings == null || cachedSuspectEmbeddings.Count < 1)
                    {
                        // Embeddings not ready yet; skip this cycle
                        embeddingsSnapshot = null;
                    }
                    else
                    {
                        // Make a snapshot for safe usage
                        embeddingsSnapshot = new Dictionary<string, float[]>(cachedSuspectEmbeddings);
                    }
                }

                if (embeddingsSnapshot != null && !workersInitialized)
                {
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

                    foreach (var img in allImageFiles)
                    {
                        imageQueue.Enqueue(img);
                    }

                    var sharedDetector = new FaceDetector();
                    var sharedEmbedder = new FaceEmbedder();

                    StartImageWatcher(_paths.FramesDir);
                    workers = Enumerable.Range(0, 4)
                        .Select(_ => new ProcessingWorker(embeddingsSnapshot, _paths.ResultDir, _paths.TempResultDir, message =>
                        {
                            // logging callback
                        }, stoppingToken, sharedDetector, sharedEmbedder, _configuration))
                        .ToArray();

                    workersInitialized = true;
                }

                // Wait a little before checking again
                await Task.Delay(1000, stoppingToken);
            }
        }
        private async Task ReloadSuspectsLoop(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Reloading suspects from DB...");

                    var updatedSuspects = new Dictionary<int, (string name, List<byte[]> blobs)>();

                    using (var conn = new SqliteConnection(connStr))
                    {
                        await conn.OpenAsync(stoppingToken);
                        using var cmd = conn.CreateCommand();
                        cmd.CommandText = @"
                    SELECT suspect_id, first_name, file_blob1, file_blob2, file_blob3, file_blob4, file_blob5, updated_at
                    FROM suspects
                    WHERE updated_at > @lastSync";
                        cmd.Parameters.AddWithValue("@lastSync", _lastLoadedTime);

                        using var reader = await cmd.ExecuteReaderAsync(stoppingToken);
                        while (await reader.ReadAsync(stoppingToken))
                        {
                            int id = reader.GetInt32(0);
                            string name = reader.IsDBNull(1) ? "" : reader.GetString(1);
                            var blobs = new List<byte[]>();

                            for (int i = 2; i <= 6; i++) // image1 to image5
                            {
                                if (!reader.IsDBNull(i))
                                {
                                    string base64 = reader.GetString(i);
                                    try
                                    {
                                        byte[] blob = Convert.FromBase64String(base64);
                                        blobs.Add(blob);
                                    }
                                    catch (FormatException ex)
                                    {
                                        _logger.LogWarning("Invalid Base64 image for suspect {0}, column image{1}: {2}", id, i - 1, ex.Message);
                                    }
                                }
                            }

                            updatedSuspects[id] = (name, blobs);

                            // Track latest update time
                            var updatedAt = reader.IsDBNull(7) ? DateTime.MinValue : reader.GetDateTime(7);
                            if (updatedAt > _lastLoadedTime)
                                _lastLoadedTime = updatedAt;
                        }
                    }

                    if (updatedSuspects.Count > 0)
                    {
                        var matcher = new FaceMatcher(_configuration);
                        var newEmbeddings = await Task.Run(() =>
                            matcher.PrecomputeSuspectEmbeddingsFromBlobs(updatedSuspects, msg => _logger.LogInformation(msg)));

                        // Replace or update suspect embeddings
                        lock (_embeddingLock)
                        {
                            if (cachedSuspectEmbeddings == null)
                                cachedSuspectEmbeddings = new Dictionary<string, float[]>();

                            foreach (var key in newEmbeddings.Keys)
                            {
                                // Remove any existing embeddings for same suspect_id
                                string prefix = key.Split('-')[0] + "-";
                                foreach (var oldKey in cachedSuspectEmbeddings.Keys.Where(k => k.StartsWith(prefix)).ToList())
                                    cachedSuspectEmbeddings.Remove(oldKey);

                                cachedSuspectEmbeddings[key] = newEmbeddings[key];
                            }
                        }

                        _logger.LogInformation("Reloaded {0} suspects at {1}", newEmbeddings.Count, DateTime.Now);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in ReloadSuspectsLoop");
                }

                await Task.Delay(TimeSpan.FromMinutes(_appSettings.SuspectReloadIntervalMinutes), stoppingToken);
            }
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
                            var matcher = new FaceMatcher(_configuration);

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
    }
}
