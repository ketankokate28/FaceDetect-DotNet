using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using Timer = System.Windows.Forms.Timer;

namespace Face_Matcher_UI
{
    public partial class Form1 : Form
    {
        string suspectDir = "";
        string imageDir = "";
        string resultDir = Path.Combine("..", "..", "..", "..", "..", "Results");
        private string[] imageFiles;
        private int currentIndex = 0;
        string fullPath = "";
        private FileSystemWatcher watcher;
        private Timer imageCheckTimer;
        public Form1()
        {
            InitializeComponent();
            comboBox1.SelectedItem = "Directory";
            comboBox2.SelectedItem = "Directory";
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            //txtLog.Location = new Point(pictureBox1.Left + 10, pictureBox1.Top);
            //txtLog.Size = new Size(300, pictureBox1.Height);

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
                    MessageBox.Show("Error loading image: " + ex.Message);
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
            if (!Directory.Exists(suspectDir) || !Directory.Exists(imageDir))
            {
                MessageBox.Show("Please select valid directories.");
                return;
            }
            Directory.CreateDirectory(resultDir);
            button1.Enabled = false;
            button1.UseVisualStyleBackColor = false; // Allow custom BackColor
            button1.BackColor = Color.Red; // Red when disabled

            var matcher = new FaceMatcher();

            Task.Run(() =>
            {
                matcher.Run(suspectDir, imageDir, fullPath, message =>
                {
                    txtLog.Invoke((MethodInvoker)(() => txtLog.AppendText(message + Environment.NewLine)));
                });

                txtLog.Invoke((MethodInvoker)(() =>
                {
                    button1.Enabled = true;
                    button1.BackColor = SystemColors.Control; // Reset to default system color
                }));
            });


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
                    }
                }
            }
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
