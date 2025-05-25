using FaceONNX;
using Microsoft.Data.Sqlite;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Numerics;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using UMapx.Visualization;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using Timer = System.Windows.Forms.Timer;

namespace Face_Matcher_UI
{
    public partial class Form1 : Form
    {
        string suspectDir = "";
        string imageDir = "";
        string resultDir = Path.Combine(AppContext.BaseDirectory, "Results");
        string tempResultDir = Path.Combine(AppContext.BaseDirectory, "Results_Temp");
        private string[] imageFiles;
        private int currentIndex = 0;
        string fullPath = "";
        private FileSystemWatcher watcher;
        private Timer imageCheckTimer;
        private Dictionary<string, float[]> cachedSuspectEmbeddings = null;
        ConcurrentQueue<string> imageQueue = new ConcurrentQueue<string>();
        ConcurrentDictionary<string, bool> processedImages = new ConcurrentDictionary<string, bool>();
        CancellationTokenSource cts = new CancellationTokenSource();
        string connectionString = "Data Source=face_match.db";
        public Form1()
        {
            InitializeComponent();
            comboBox1.SelectedItem = "Directory";
            comboBox2.SelectedItem = "Directory";
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            btnPrevious.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnNext.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            label1.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            //txtLog.Location = new Point(pictureBox1.Left + 10, pictureBox1.Top);
            //txtLog.Size = new Size(300, pictureBox1.Height);
           // pictureLoading1.Image = Properties.Resources.loader1;
            txtLog.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            txtLog.Width = this.ClientSize.Width - txtLog.Left - 10;
            fullPath = Path.GetFullPath(resultDir);

            // Create directory if it doesn't exist
            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
            }
            else
            {
                //Array.ForEach(Directory.GetFiles(resultDir), File.Delete); 
            }
            watcher = new FileSystemWatcher(fullPath);
            watcher.Filter = "*.*";
            watcher.IncludeSubdirectories = false;
            watcher.EnableRaisingEvents = true;
            watcher.Created += Watcher_Created;
            watcher.Deleted += Watcher_Deleted;

            imageCheckTimer = new System.Windows.Forms.Timer();
            imageCheckTimer.Interval = 2000; // check every 2 seconds
            imageCheckTimer.Tick += ImageCheckTimer_Tick;
            imageCheckTimer.Start();

            label1.AutoSize = true;

            //label1.Location = new Point(btnPrevious_Click_1.Bottom, pictureBox1.Bottom + 10);

            imageFiles = Directory.GetFiles(fullPath, "*.*")
                                  .Where(f => IsImageFile(f))
                                  .OrderBy(f => f) // sort by filename ascending
                                  .ToArray();
            PrecomputeSuspectEmbeddingsAsync(Path.Combine(AppContext.BaseDirectory, "suspects"));

        }

        private Dictionary<int, string> LoadSuspectList()
        {
            var suspects = new Dictionary<int, string>();

            try
            {
                using (var connection = new SqliteConnection(connectionString))
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


        private void Watcher_Deleted(object sender, FileSystemEventArgs e)
        {
            this.Invoke((MethodInvoker)(() =>
            {
                imageFiles = Directory.GetFiles(fullPath, "*.*")
                      .Where(f => IsImageFile(f))
                      .OrderBy(f => f) // sort by filename ascending
                      .ToArray();

                if (currentIndex >= imageFiles.Length)
                {
                    currentIndex = imageFiles.Length - 1;
                }

                ShowImage();
            }));
        }

        private void ImageCheckTimer_Tick(object sender, EventArgs e)
        {
            if (!Directory.Exists(fullPath)) return;

            var updatedFiles = Directory.GetFiles(fullPath, "*.*")
                                        .Where(f => f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                                                    f.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                                                    f.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase) ||
                                                    f.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
                                        .ToArray();

            // If files changed (added or deleted)
            if (imageFiles == null || imageFiles.Length != updatedFiles.Length || !imageFiles.SequenceEqual(updatedFiles))
            {
                imageFiles = updatedFiles;

                // Adjust current index if necessary
                if (currentIndex >= imageFiles.Length)
                {
                    currentIndex = imageFiles.Length - 1;
                }

                if (imageFiles.Length > 0)
                {
                    ShowImage();
                }
                else
                {
                    pictureBox1.Image = null;
                    label1.Text = "";
                }
            }
        }
        private void Watcher_Created(object sender, FileSystemEventArgs e)
        {
            if (!IsImageFile(e.FullPath)) return;

            // Give OS time to complete write
            Thread.Sleep(1500);

            this.Invoke((MethodInvoker)(() =>
            {
                var files = Directory.GetFiles(fullPath, "*.*")
                                     .Where(f => IsImageFile(f))
                                     .ToArray();

                if (files.Length == 0) return;

                imageFiles = files;
                currentIndex = imageFiles.Length - 1;
                ShowImage();
            }));
        }

        private bool IsImageFile(string file)
        {
            string ext = Path.GetExtension(file).ToLower();
            return ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".bmp";
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            imageFiles = Directory.GetFiles(fullPath, "*.*")
                                  .Where(f => f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                                              f.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                                              f.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase) ||
                                              f.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
                                  .OrderBy(f => f)
                                  .ToArray();

            if (imageFiles.Length > 0)
            {
                currentIndex = 0;
                ShowImage();
            }
        }

        private void ShowImage()
        {
            if (imageFiles == null || imageFiles.Length == 0 || currentIndex < 0 || currentIndex >= imageFiles.Length)
            {
                if (pictureBox1.Image != null)
                {
                    pictureBox1.Image.Dispose();
                    pictureBox1.Image = null;
                }
                label1.Text = "No images";
                return;
            }
            if (pictureBox1.Image != null)
            {
                pictureBox1.Image.Dispose();
                pictureBox1.Image = null;
            }
            string file = imageFiles[currentIndex];
            int maxRetries = 3;
            int delayMs = 300;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (MemoryStream ms = new MemoryStream())
                    {
                        fs.CopyTo(ms);
                        ms.Seek(0, SeekOrigin.Begin);

                        // Dispose previous image before assigning new one
                        if (pictureBox1.Image != null)
                        {
                            pictureBox1.Image.Dispose();
                            pictureBox1.Image = null;
                        }

                       pictureBox1.Image = Image.FromStream(ms);
                    }

                    label1.Text = $"Image {currentIndex + 1} of {imageFiles.Length}";
                    return;
                }
                catch (IOException)
                {
                    Thread.Sleep(delayMs); // Wait and retry
                }
                catch (Exception ex)
                {
                   // MessageBox.Show("Error loading image: " + ex.Message);
                    return;
                }
            }
        }



        private void btnNext_Click(object sender, EventArgs e)
        {
            if (imageFiles != null && imageFiles.Length > 0)
            {
                currentIndex = (currentIndex + 1) % imageFiles.Length;
                ShowImage();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if(cachedSuspectEmbeddings.Count <1 || !Directory.Exists(imageDir))
            {
                MessageBox.Show("Please select valid directories for suspect or images.");
                return;
            }
            if (!Directory.Exists(tempResultDir))
            {
                Directory.CreateDirectory(tempResultDir);
            }
            else
            {
                Array.ForEach(Directory.GetFiles(resultDir), File.Delete);
            }
            button1.Enabled = false;
            button1.UseVisualStyleBackColor = false;
            button1.BackColor = Color.Red;

            var matcher = new FaceMatcher();
            if (cachedSuspectEmbeddings == null)
            {
                MessageBox.Show("Please load suspect embeddings first.");
                return;
            }
            var allImageFiles = Directory.GetFiles(imageDir, "*.*")
        .Where(IsImage)
        .ToArray();

            foreach (var img in allImageFiles)
            {
                if (!processedImages.ContainsKey(img))
                {
                    imageQueue.Enqueue(img);
                }
            }

            StartImageWatcher(imageDir);
            StartProcessingLoop(cachedSuspectEmbeddings);
            txtLog.AppendText("Started monitoring and processing...\n");
            //            Task.Run(() =>
            //            {
            //                var stopwatch = Stopwatch.StartNew(); // Start timing

            //                var allImageFiles = Directory.GetFiles(imageDir);

            //                int totalFiles = allImageFiles.Length;
            //                int half = (int)Math.Ceiling(totalFiles / 2.0);

            //                var batches = new List<string[]>
            //{
            //    allImageFiles.Take(half).ToArray(),
            //    allImageFiles.Skip(half).ToArray()
            //};

            //                Parallel.ForEach(batches, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, batch =>
            //                {
            //                    matcher.RunBatch(cachedSuspectEmbeddings, batch, resultDir, message =>
            //                    {
            //                        txtLog.Invoke((MethodInvoker)(() => txtLog.AppendText(message + Environment.NewLine)));
            //                    });
            //                });

            //                stopwatch.Stop();  // Stop after all processing is done

            //                txtLog.Invoke((MethodInvoker)(() =>
            //                {
            //                    txtLog.AppendText($"\nTotal processing time: {stopwatch.Elapsed.TotalSeconds:F2} seconds\n");
            //                    button1.Enabled = true;
            //                    button1.BackColor = SystemColors.Control;
            //                }));
            //            });

            txtLog.Invoke((MethodInvoker)(() => txtLog.AppendText("\nTotal processing time: {stopwatch.Elapsed.TotalSeconds:F2} seconds")));

        }
        void StartProcessingLoop(Dictionary<string, float[]> cachedSuspectEmbeddings)
        {
            var matcher = new FaceMatcher();
            List<string> currentBatch = new List<string>();
            var sw = Stopwatch.StartNew();

            Task.Run(() =>
            {
               

                DateTime lastBatchTime = DateTime.UtcNow;

                while (!cts.Token.IsCancellationRequested)
                {
                 
                    bool newItemAdded = false;

                    while (imageQueue.TryDequeue(out string imageFile))
                    {
                        currentBatch.Add(imageFile);
                        newItemAdded = true;
                        //if (!processedImages.ContainsKey(imageFile))
                        //{
                        //    currentBatch.Add(imageFile);
                        //    newItemAdded = true;
                        //}

                        if (currentBatch.Count >= 1000)
                            break; // Stop if batch is large enough
                    }

                    bool timeToFlush = (DateTime.UtcNow - lastBatchTime).TotalSeconds >= 1;

                    if (currentBatch.Count > 0 && (currentBatch.Count >= 1000 || timeToFlush))
                    {

                        matcher.RunBatch(cachedSuspectEmbeddings, currentBatch.ToArray(), resultDir,tempResultDir, message =>
                        {
                            txtLog.Invoke((MethodInvoker)(() =>
                                txtLog.AppendText(message + $" \nTotal processing time: {sw.Elapsed}")));
                        });

                        //foreach (var file in currentBatch)
                        //    processedImages.TryAdd(file, true);

                        currentBatch.Clear();
                        lastBatchTime = DateTime.UtcNow;
                    }

                    if (!newItemAdded)
                        Thread.Sleep(500); // Avoid busy looping if queue is empty
                }
            }, cts.Token);
        }

        private FileSystemWatcher watcher1;
        void StartImageWatcher(string imageDir)
        {
            watcher1 = new FileSystemWatcher(imageDir);
            watcher1.IncludeSubdirectories = false;
            watcher1.Filter = "*.*";
            watcher1.NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime;

            watcher1.Created += (s, e) =>
            {
                if (IsImage(e.FullPath) && !processedImages.ContainsKey(e.FullPath))
                {
                    imageQueue.Enqueue(e.FullPath);
                    Console.WriteLine($"File created and queued: {e.FullPath}");
                }
            };

            watcher1.EnableRaisingEvents = true;
        }
        bool IsImage(string path)
        {
            string ext = Path.GetExtension(path).ToLower();
            return ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".bmp";
        }

        private void btnBrowseSuspect_Click(object sender, EventArgs e)
        {
            var selectedType = comboBox1.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(selectedType))
            {
                MessageBox.Show("Please select File or Directory from the dropdown.");
                return;
            }
            openFileDialog1.Title = "Select a file";
            openFileDialog1.Filter = "All files (*.*)|*.*";

            if (selectedType == "File")
            {
                using (OpenFileDialog openFileDialog = new OpenFileDialog())
                {
                    openFileDialog.Title = "Select a file";
                    openFileDialog.Filter = "All files (*.*)|*.*";
                    if (openFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        suspectDir = openFileDialog.FileName;

                    }
                }
            }
            else if (selectedType == "Directory")
            {
                using (FolderBrowserDialog folderDialog = new FolderBrowserDialog())
                {
                    folderDialog.Description = "Select a folder";
                    if (folderDialog.ShowDialog() == DialogResult.OK)
                    {
                        suspectDir = folderDialog.SelectedPath;
                        cachedSuspectEmbeddings = null;
                        PrecomputeSuspectEmbeddingsAsync(folderDialog.SelectedPath);
                    }
                }
            }
        }
        private async void PrecomputeSuspectEmbeddingsAsync(string suspectDir)
        {
            //pictureLoading1.Visible = true;
            this.Enabled = false;
            Cursor.Current = Cursors.WaitCursor;
            txtLog.AppendText("Precomputing suspect embeddings...\n");
            Dictionary<int, string> suspectList = LoadSuspectList();
            await Task.Run(() =>
            {
                var matcher = new FaceMatcher();

                // Filter only folders matching suspect IDs

                var validSuspectFolders = Directory.GetDirectories(suspectDir)
                    .Where(folderPath =>
                    {
                        var folderName = Path.GetFileName(folderPath);
                        return int.TryParse(folderName, out int id) && suspectList.ContainsKey(id);
                    })
                    .ToList();

                // Call matcher logic only with valid folders
                cachedSuspectEmbeddings = matcher.PrecomputeSuspectEmbeddings(validSuspectFolders, message =>
                {
                    txtLog.Invoke((MethodInvoker)(() => txtLog.AppendText(message + Environment.NewLine)));
                });
            });

            //pictureLoading1.Visible = false;
            txtLog.AppendText("Suspect embeddings ready.\n");
            this.Enabled = true;
            Cursor.Current = Cursors.Default;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            var selectedType = comboBox2.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(selectedType))
            {
                MessageBox.Show("Please select File or Directory from the dropdown.");
                return;
            }
            openFileDialog1.Title = "Select a file";
            openFileDialog1.Filter = "All files (*.*)|*.*";

            if (selectedType == "File")
            {
                using (OpenFileDialog openFileDialog = new OpenFileDialog())
                {
                    openFileDialog.Title = "Select a file";
                    openFileDialog.Filter = "All files (*.*)|*.*";
                    if (openFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        imageDir = openFileDialog.FileName;
                    }
                }
            }
            else if (selectedType == "Directory")
            {
                using (FolderBrowserDialog folderDialog = new FolderBrowserDialog())
                {
                    folderDialog.Description = "Select a folder";
                    if (folderDialog.ShowDialog() == DialogResult.OK)
                    {
                        imageDir = folderDialog.SelectedPath;
                    }
                }
            }
        }

        private void btnPrevious_Click_1(object sender, EventArgs e)
        {
            if (imageFiles != null && imageFiles.Length > 0)
            {
                currentIndex = (currentIndex - 1 + imageFiles.Length) % imageFiles.Length;
                ShowImage();
            }
        }

        private void btnNext_Click_1(object sender, EventArgs e)
        {
            if (imageFiles != null && imageFiles.Length > 0)
            {
                currentIndex = (currentIndex + 1) % imageFiles.Length;
                ShowImage();
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void enrollSuspectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var selectedType = comboBox1.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(selectedType))
            {
                MessageBox.Show("Please select File or Directory from the dropdown.");
                return;
            }
            openFileDialog1.Title = "Select a file";
            openFileDialog1.Filter = "All files (*.*)|*.*";

            if (selectedType == "File")
            {
                using (OpenFileDialog openFileDialog = new OpenFileDialog())
                {
                    openFileDialog.Title = "Select a file";
                    openFileDialog.Filter = "All files (*.*)|*.*";
                    if (openFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        suspectDir = openFileDialog.FileName;
                    }
                }
            }
            else if (selectedType == "Directory")
            {
                using (FolderBrowserDialog folderDialog = new FolderBrowserDialog())
                {
                    folderDialog.Description = "Select a folder";
                    if (folderDialog.ShowDialog() == DialogResult.OK)
                    {
                        suspectDir = folderDialog.SelectedPath;
                    }
                }
            }
        }

        private void addImagesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var selectedType = comboBox2.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(selectedType))
            {
                MessageBox.Show("Please select File or Directory from the dropdown.");
                return;
            }
            openFileDialog1.Title = "Select a file";
            openFileDialog1.Filter = "All files (*.*)|*.*";

            if (selectedType == "File")
            {
                using (OpenFileDialog openFileDialog = new OpenFileDialog())
                {
                    openFileDialog.Title = "Select a file";
                    openFileDialog.Filter = "All files (*.*)|*.*";
                    if (openFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        imageDir = openFileDialog.FileName;
                    }
                }
            }
            else if (selectedType == "Directory")
            {
                using (FolderBrowserDialog folderDialog = new FolderBrowserDialog())
                {
                    folderDialog.Description = "Select a folder";
                    if (folderDialog.ShowDialog() == DialogResult.OK)
                    {
                        imageDir = folderDialog.SelectedPath;
                    }
                }
            }
        }
    }
}
