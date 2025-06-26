using System;
using System.Drawing;
using System.Windows.Forms;
using UI.Controls;
using System.Collections.Generic;
using System.Linq;
using Face_Matcher_UI;
using SuspectManager.Forms;
using Model;
using System.Collections.Concurrent;
using FaceONNX;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using Button = System.Windows.Forms.Button;

namespace UI
{
    public partial class SuspectListControl : UserControl
    {
        private Panel panelDetails;
        private List<(int Id, string Name, Image Img)> allSuspects;
        private bool isEditingSuspect = false;
        private bool isPopupOpen = false;
        private string selectedFolderPath = null;
        private FileSystemWatcher watcher1;
        private StandaloneProcessingWorker[] workers;
        private int workerIndex = 0;
        private readonly object workerLock = new object();
        private readonly object _embeddingLock = new();
        private Dictionary<string, float[]> cachedSuspectEmbeddings = null;
        ConcurrentQueue<string> imageQueue = new ConcurrentQueue<string>();
        private int _selectedSuspectId = -1;
        string ResultDir = Path.Combine(AppContext.BaseDirectory, "Results");
        string FramesDir = Path.Combine(AppContext.BaseDirectory, "frames");
        string TempResultDir = Path.Combine(AppContext.BaseDirectory, "Results_Temp");
        string TempProcessDir = Path.Combine(AppContext.BaseDirectory, "Temp_Process");
        private CancellationTokenSource _cts;
        ConcurrentDictionary<string, bool> processedImages = new ConcurrentDictionary<string, bool>();
        private List<string> matchedBase64Images = new();
        private int currentMatchImageIndex = 0;
        private PictureBox matchImageViewer = new();
        private Button btnNext;
        private Button btnPrev;
        private Label lblImageCounter;
        string[] imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".bmp" };
        private Task _searchTask;
        private bool isProcessingRunning = false;
        public bool IsProcessingRunning => isProcessingRunning;
        private Panel loaderPanel;
        private Label loaderTextLabel;
        private System.Windows.Forms.Timer searchTimer;
        private DateTime searchStartTime;
        private int dotCount = 0;
        public SuspectListControl()
        {
            InitializeComponent();

            panelDetails.Controls.Clear();
            LoadSuspectCards();
            splitContainer.SplitterDistance = splitContainer.SplitterDistance + 1;
            splitContainer.SplitterDistance = splitContainer.SplitterDistance - 1;
            splitContainer.BorderStyle = BorderStyle.Fixed3D;
            splitContainer.Panel2MinSize = 1;
            this.Load += SuspectListControl_Load;

        }

        private void SuspectListControl_Load(object sender, EventArgs e)
        {
            ShowMatchImage(0); // This initializes the image view, label, buttons etc. correctly
        }
        private void LoadSuspectCards()
        {
            var dbSuspects = DbHelper.GetAllSuspects();
            allSuspects = new List<(int, string, Image)>();

            foreach (var s in dbSuspects)
            {
                string fullName = $"{s.FirstName} {s.LastName}".Trim();

                // Pick the first non-empty base64 image
                string firstBase64 = s.Images.FirstOrDefault(b64 => !string.IsNullOrWhiteSpace(b64));
                Image img = null;

                if (!string.IsNullOrWhiteSpace(firstBase64))
                {
                    try
                    {
                        byte[] imageBytes = Convert.FromBase64String(firstBase64);
                        using var ms = new System.IO.MemoryStream(imageBytes);
                        img = Image.FromStream(ms);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Base64 image decode failed for suspect ID {s.SuspectId}: {ex.Message}");
                    }
                }

                // Optional fallback to default avatar if needed
                // if (img == null)
                //     img = Properties.Resources.default_avatar;

                allSuspects.Add((s.SuspectId, fullName, img));
            }

            DisplaySuspects(allSuspects);
        }


        private void DisplaySuspects(List<(int Id, string Name, Image Img)> suspects)
        {
            flowLayoutPanelSuspects.Controls.Clear();

            foreach (var s in suspects)
            {
                var card = new SuspectCard();
                card.ClearEditClickHandlers();
                card.SuspectId = s.Id;
                card.SetData(s.Name, s.Img);

                int capturedId = s.Id; // capture loop variable safely

                // Card click opens details
                card.CardClicked += (sender, e) =>
                {
                    if (isProcessingRunning)
                    {
                        MessageBox.Show("Please stop the current search before changing suspect.", "Search Running", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    _selectedSuspectId = capturedId;  // <-- Save globally
                    ShowSuspectDetails(capturedId);
                };

                // Edit click opens form
                card.EditClicked += (sender, e) =>
                {
                    if (isProcessingRunning)
                    {
                        MessageBox.Show("Please stop the current search before editing a suspect.", "Search Running", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    if (isPopupOpen) return;
                    if (isEditingSuspect) return;

                    isEditingSuspect = true;
                    isPopupOpen = true;

                    if (sender is SuspectCard clickedCard)
                    {
                        // Important: remove focus from edit label to prevent repeat click
                        clickedCard.Parent.Focus();

                        var suspect = DbHelper.GetAllSuspects().FirstOrDefault(x => x.SuspectId == capturedId);
                        if (suspect == null)
                        {
                            isEditingSuspect = false;
                            isPopupOpen = false;
                            return;
                        }

                        using var form = new EnrollSuspectForm(suspect);
                        var result = form.ShowDialog();
                        isPopupOpen = false;
                        isEditingSuspect = false;
                        if (result == DialogResult.OK)
                        {
                            DbHelper.InsertOrUpdateSuspect(form.Suspect);
                            LoadSuspectCards();
                        }
                    }
                };

                flowLayoutPanelSuspects.Controls.Add(card);
            }
        }

        private void ShowSuspectDetails(int suspectId)
        {
            selectedFolderPath = null;
            panelDetails.Controls.Clear();
            var suspect = DbHelper.GetAllSuspects().FirstOrDefault(s => s.SuspectId == suspectId);
            if (suspect == null) return;

            int padding = 20;

            string genderDisplay = suspect.Gender?.Trim().ToUpper() switch
            {
                "M" => "Male",
                "F" => "Female",
                _ => "Other"
            };

            // === LEFT CONTENT PANEL ===
            var leftContent = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(panelDetails.Width - 280, panelDetails.Height),
                AutoScroll = true,
                BackColor = Color.White
            };

            AddTopBarWithButtons(leftContent, suspect, genderDisplay, padding);
            AddImageThumbnailScroller(leftContent, suspect, padding);
            AddImagePreviewPanel(leftContent, padding);

            panelDetails.Controls.Add(leftContent);

            // === RIGHT PANEL (DOB, FIR, etc) ===
            AddSuspectDetailsPanel(suspect);
            LoadMatchedFramesPanel(leftContent, _selectedSuspectId);
        }
        private void LoadMatchedFramesPanel(Control parent, int suspectId)
        {
            matchedBase64Images = DbHelper.GetMatchedFramesForSuspect(suspectId);
            currentMatchImageIndex = 0;

            // Get the left preview panel from Tag (set in AddImagePreviewPanel)
            if (panelImagePreview.Tag is Panel leftPreviewPanel)
            {
                leftPreviewPanel.Controls.Clear();

                matchImageViewer = new PictureBox
                {
                    Dock = DockStyle.Fill,
                    SizeMode = PictureBoxSizeMode.Zoom,
                    BorderStyle = BorderStyle.FixedSingle
                };
                leftPreviewPanel.Controls.Add(matchImageViewer);

                ShowMatchImage(0); // Show the first match image
            }

            // Get the center button column and add vertical buttons (between left and right image)
            if (panelImagePreview.Controls[0] is TableLayoutPanel layout && layout.ColumnCount == 3)
            {
                // Remove any existing control in center column
                if (layout.GetControlFromPosition(1, 0) is Control oldButtonPanel)
                    layout.Controls.Remove(oldButtonPanel);

                // Vertical button panel
                // Vertical button panel
                var verticalButtonPanel = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    RowCount = 3,
                    ColumnCount = 1,
                    BackColor = Color.WhiteSmoke
                };
                verticalButtonPanel.RowStyles.Clear();
                verticalButtonPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 40));
                verticalButtonPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40)); // counter label fixed
                verticalButtonPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 40));


                btnPrev = new Button
                {
                    Text = "▲",
                    Font = new Font("Segoe UI", 14, FontStyle.Bold),
                    Anchor = AnchorStyles.None,
                    Width = 50,
                    Height = 50
                };
                btnPrev.Click += (s, e) => ShowMatchImage(-1);

                btnNext = new Button
                {
                    Text = "▼",
                    Font = new Font("Segoe UI", 14, FontStyle.Bold),
                    Anchor = AnchorStyles.None,
                    Width = 50,
                    Height = 50
                };
                btnNext.Click += (s, e) => ShowMatchImage(1);

                // Counter label in the middle
                lblImageCounter = new Label
                {
                    Text = "",
                    TextAlign = ContentAlignment.MiddleCenter,
                    Dock = DockStyle.Fill,
                    Font = new Font("Segoe UI", 11, FontStyle.Bold),
                    ForeColor = Color.Black,
                    AutoSize = false,
                    Padding = new Padding(0),
                    Margin = new Padding(0),
                    Anchor = AnchorStyles.None
                };

                verticalButtonPanel.Controls.Add(btnPrev, 0, 0);
                verticalButtonPanel.Controls.Add(lblImageCounter, 0, 1);
                verticalButtonPanel.Controls.Add(btnNext, 0, 2);

                layout.Controls.Add(verticalButtonPanel, 1, 0); // center column
                layout.PerformLayout();
            }
        }

        private void ShowMatchImage(int delta)
        {
            if (matchedBase64Images == null || matchedBase64Images.Count == 0)
            {
                if (matchImageViewer != null)
                    matchImageViewer.Image = null;

                if (btnPrev != null) btnPrev.Enabled = false;
                if (btnNext != null) btnNext.Enabled = false;
                if (lblImageCounter != null) lblImageCounter.Text = "0 / 0";

                // Optional: Force layout refresh to align buttons and labels
                btnPrev?.Refresh();
                btnNext?.Refresh();
                lblImageCounter?.Refresh();
                this.PerformLayout();

                return;
            }

            int total = matchedBase64Images.Count;
            currentMatchImageIndex = Math.Max(0, Math.Min(currentMatchImageIndex + delta, total - 1));

            try
            {
                var b64 = matchedBase64Images[currentMatchImageIndex];
                byte[] imageBytes = Convert.FromBase64String(b64);
                using var ms = new System.IO.MemoryStream(imageBytes);

                matchImageViewer?.Image?.Dispose();
                if (matchImageViewer != null)
                    matchImageViewer.Image = new Bitmap(Image.FromStream(ms));
            }
            catch
            {
                if (matchImageViewer != null)
                    matchImageViewer.Image = null;
            }

            if (btnPrev != null) btnPrev.Enabled = currentMatchImageIndex > 0;
            if (btnNext != null) btnNext.Enabled = currentMatchImageIndex < total - 1;

            if (lblImageCounter != null)
                lblImageCounter.Text = $"{currentMatchImageIndex + 1} / {total}";

            btnPrev?.Refresh();
            btnNext?.Refresh();
            lblImageCounter?.Refresh();
            this.PerformLayout();
        }

        private void AddTopBarWithButtons(Control parent, dynamic suspect, string genderDisplay, int padding)
        {
            var topHeaderPanel = new Panel
            {
                Location = new Point(padding, padding),
                Size = new Size(parent.Width - 2 * padding, 45),
                AutoSize = false,
                BackColor = Color.White
            };

            var nameGenderLabel = new Label
            {
                Text = $"{suspect.FirstName} {suspect.LastName} ({genderDisplay})",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(0, 10)
            };
            topHeaderPanel.Controls.Add(nameGenderLabel);

            // === Flow panel for buttons ===
            var flow = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                Location = new Point(nameGenderLabel.Right + 20, 6),
                Padding = new Padding(0),
                Margin = new Padding(0)
            };

            // === Buttons ===
            var btnSelectFolder = new Button
            {
                Text = "📁 Select Source Folder",
                AutoSize = true,
                Height = 30,
                Margin = new Padding(10, 0, 10, 0)
            };

            var btnStart = new Button
            {
                Text = "▶ Start Search",
                AutoSize = true,
                Height = 30,
                Margin = new Padding(10, 0, 10, 0)
            };

            var btnStop = new Button
            {
                Text = "⏹ Stop Search",
                AutoSize = true,
                Height = 30,
                Enabled = false,
                Margin = new Padding(10, 0, 10, 0)
            };

            var btnExport = new LinkLabel
            {
                Text = "Export Search Report",
                AutoSize = true,
                Enabled = true,
                Margin = new Padding(15, 7, 0, 0)
            };
            // Loader Panel (Modern pill look)
            loaderPanel = new Panel
            {
                Width = 200,
                Height = 30,
                Margin = new Padding(5, 5, 5, 0),
                Padding = new Padding(0),
                Visible = false,
                BackColor = Color.WhiteSmoke,
                BorderStyle = BorderStyle.None,
            };

            // Add soft shadow effect (simulate with border)
            loaderPanel.Paint += (s, e) =>
            {
                ControlPaint.DrawBorder(e.Graphics, loaderPanel.ClientRectangle,
                    Color.LightGray, 1, ButtonBorderStyle.Solid,
                    Color.LightGray, 1, ButtonBorderStyle.Solid,
                    Color.LightGray, 1, ButtonBorderStyle.Solid,
                    Color.LightGray, 1, ButtonBorderStyle.Solid);
            };

            // Rounded corners (pill-style)
            loaderPanel.Region = System.Drawing.Region.FromHrgn(
                NativeMethods.CreateRoundRectRgn(0, 0, loaderPanel.Width, loaderPanel.Height, 20, 20)
            );

            loaderTextLabel = new Label
            {
                Text = "🔍 Searching...",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                AutoSize = false,
                ForeColor = Color.MediumVioletRed, // softer standout
                BackColor = Color.Transparent,
            };

            loaderPanel.Controls.Add(loaderTextLabel);

            // === Logic ===
            btnSelectFolder.Click += (s, e) =>
            {
                using var dialog = new FolderBrowserDialog();
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    selectedFolderPath = dialog.SelectedPath;
                    //MessageBox.Show($"Folder selected: {selectedFolderPath}");
                }
            };

            btnStart.Click += (s, e) =>
            {
                HandleStartSearch(btnStart, btnStop, btnExport);
            };

            btnStop.Click += (s, e) =>
            {
                HandleStopSearch(btnStart, btnStop, btnExport);
            };

            btnExport.Click += (s, e) =>
            {
                HandleExportReport();
            };

            // Add buttons to flow
            StyleActionButton(btnSelectFolder, Color.WhiteSmoke, Color.Black, Color.Gainsboro, Color.LightGray);
            StyleActionButton(btnStart, Color.WhiteSmoke, Color.Black, Color.Gainsboro, Color.LightGray);
            StyleActionButton(btnStop, Color.WhiteSmoke, Color.Black, Color.Gainsboro, Color.LightGray);


            flow.Controls.Add(btnSelectFolder);
            flow.Controls.Add(btnStart);
            flow.Controls.Add(btnStop);

            flow.Controls.Add(loaderPanel);
            flow.Controls.Add(btnExport);

            topHeaderPanel.Controls.Add(flow);
            parent.Controls.Add(topHeaderPanel);
        }

        private async void HandleStartSearch(Button btnStart, Button btnStop, LinkLabel btnExport)
        {
            if (string.IsNullOrEmpty(selectedFolderPath) || !System.IO.Directory.Exists(selectedFolderPath))
            {
                MessageBox.Show("Please select a valid source folder before starting the search.", "Missing Folder", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_selectedSuspectId <= 0)
            {
                MessageBox.Show("Please select suspect.");
            }
            isProcessingRunning = true;
            btnStart.Enabled = false;
            btnStop.Enabled = true;
            btnExport.Enabled = false;

            loaderPanel.Visible = true;
            searchStartTime = DateTime.Now;
            dotCount = 0;

            if (searchTimer == null)
            {
                searchTimer = new System.Windows.Forms.Timer();
                searchTimer.Interval = 1000;
                searchTimer.Tick += (s, e) =>
                {
                    var elapsed = DateTime.Now - searchStartTime;
                    dotCount = (dotCount + 1) % 4;
                    string dots = new string('.', dotCount);
                    loaderTextLabel.Text = $"🔍 Searching{dots}  ({elapsed.Minutes:D2}:{elapsed.Seconds:D2})";
                };
            }
            searchTimer.Start();

            //if (loaderLabel != null)
            //    loaderLabel.Visible = true;
            // Step 1: Reload suspect
            await ReloadSingleSuspect(_selectedSuspectId);

            // Step 2: Start processing in background with cancellation support
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            Task.Run(() => Process(token), token);
            // TODO: Add actual search start logic here
            //MessageBox.Show($"Search started on folder:\n{selectedFolderPath}");
        }
        private async Task Process(CancellationToken stoppingToken)
        {
            if (!Directory.Exists(TempResultDir))
            {
                Directory.CreateDirectory(TempResultDir);
            }
            else
            {
                Array.ForEach(Directory.GetFiles(TempResultDir), File.Delete);
            }

            var allImageFiles = Directory.GetFiles(selectedFolderPath)
                .Where(f => IsImage(f))
                .ToArray();

            var sharedDetector = new FaceDetector();
            var sharedEmbedder = new FaceEmbedder();
            workers = Enumerable.Range(0, 4)
                .Select(_ => new StandaloneProcessingWorker(cachedSuspectEmbeddings, ResultDir, TempResultDir, message =>
                {
                    // logging callback
                }, stoppingToken, sharedDetector, sharedEmbedder))
                .ToArray();
            CopyAllImagesToFolder(selectedFolderPath, FramesDir);
            //foreach (var img in allImageFiles)
            //{
            //    if (!processedImages.ContainsKey(img))
            //    {
            //        processedImages.TryAdd(img, true);
            //        EnqueueImage(img);

            //    }
            //}
        }
        void CopyAllImagesToFolder(string sourceDir, string destDir)
        {
            if (!Directory.Exists(destDir))
            {
                Directory.CreateDirectory(destDir);
            }
            else
            {
                Directory.GetFiles(destDir).ToList().ForEach(File.Delete);
            }

            var allImageFiles = Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories)
                .Where(IsImage)
                .ToArray();

            foreach (var file in allImageFiles)
            {
                var destPath = Path.Combine(destDir, Path.GetFileName(file));

                // Avoid overwrite conflicts
                string finalPath = destPath;
                int count = 1;
                while (File.Exists(finalPath))
                {
                    string fileName = Path.GetFileNameWithoutExtension(file);
                    string ext = Path.GetExtension(file);
                    finalPath = Path.Combine(destDir, $"{fileName}_{count}{ext}");
                    count++;
                }

                File.Copy(file, finalPath);
                if (!processedImages.ContainsKey(finalPath))
                {
                    processedImages.TryAdd(finalPath, true);
                    EnqueueImage(finalPath);

                }
            }
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
            return imageExtensions.Contains(ext);
        }
        private async void HandleStopSearch(Button btnStart, Button btnStop, LinkLabel btnExport)
        {
            btnStart.Enabled = true;
            btnStop.Enabled = false;
            btnExport.Enabled = true;
            selectedFolderPath = string.Empty;
            //if (loaderLabel != null)
            //    loaderLabel.Visible = false;

            searchTimer?.Stop();
            loaderPanel.Visible = false;
            loaderTextLabel.Text = "🔍 Searching...";


            // Optional: reset text for next run
            loaderTextLabel.Text = "Searching...";
            _cts?.Cancel();

            // Show popup while waiting
            using (var stopForm = new StopProgressForm())
            {
                stopForm.ShowDialog();  // Blocks until progress completes
            }
            if (_searchTask != null)
            {
                try
                {
                    await _searchTask;
                }
                catch (OperationCanceledException)
                {
                    // Expected during cancellation
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error during cancellation: {ex.Message}");
                }
            }
            _cts?.Dispose();
            _cts = null;
            _searchTask = null;
            if (workers != null)
            {
                foreach (var worker in workers)
                {
                    worker.Dispose(); // safe to dispose now
                }

                workers = null;
            }
            processedImages.Clear();
            isProcessingRunning = false;
            // TODO: Add actual stop logic here
            //MessageBox.Show("Search stopped.");
        }

        private void HandleExportReport()
        {
            // TODO: Replace with real export logic
            MessageBox.Show("Exporting Search Report...");
        }

        private void AddImageThumbnailScroller(Control parent, dynamic suspect, int padding)
        {
            imageScroll.Controls.Clear();
            imageScroll.Location = new Point(padding, 60);  // Below the top panel
            imageScroll.Size = new Size(parent.Width - 2 * padding, 110);
            imageScroll.Margin = new Padding(0, 0, 0, 20);
            imageScroll.WrapContents = false;
            imageScroll.AutoScroll = true;
            imageScroll.BorderStyle = BorderStyle.FixedSingle;

            picMainImage.Image = null;
            bool isFirstImage = true; // flag to auto-load first image

            foreach (string b64 in suspect.Images)
            {
                if (!string.IsNullOrWhiteSpace(b64))
                {
                    try
                    {
                        byte[] imgBytes = Convert.FromBase64String(b64);
                        using var ms = new System.IO.MemoryStream(imgBytes);
                        var image = Image.FromStream(ms);
                        image = new Bitmap(image);

                        var pic = new PictureBox
                        {
                            Image = image,
                            SizeMode = PictureBoxSizeMode.Zoom,
                            Size = new Size(100, 100),
                            Margin = new Padding(5),
                            Cursor = Cursors.Hand,
                            BorderStyle = BorderStyle.FixedSingle
                        };

                        pic.Click += (s, e) =>
                        {
                            picMainImage.Image?.Dispose();
                            picMainImage.Image = new Bitmap(image);
                        };

                        imageScroll.Controls.Add(pic);

                        // ✅ Auto-show first image in preview
                        if (isFirstImage)
                        {
                            picMainImage.Image?.Dispose();
                            picMainImage.Image = new Bitmap(image);
                            isFirstImage = false;
                        }
                    }
                    catch
                    {
                        // log or ignore invalid base64 images
                    }
                }
            }

            parent.Controls.Add(imageScroll);
        }
        private void StyleActionButton(Button btn, Color bgColor, Color textColor, Color hoverColor, Color borderColor)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = borderColor;
            btn.Font = new Font("Segoe UI", 9, FontStyle.Regular);
            btn.Padding = new Padding(10, 4, 10, 4);

            // === Event: hover ===
            btn.MouseEnter += (s, e) =>
            {
                if (btn.Enabled)
                {
                    btn.BackColor = hoverColor;
                    btn.ForeColor = Color.Black;
                }
            };

            btn.MouseLeave += (s, e) =>
            {
                if (btn.Enabled)
                {
                    btn.BackColor = bgColor;
                    btn.ForeColor = textColor;
                }
            };

            // === Event: enabled changed ===
            btn.EnabledChanged += (s, e) =>
            {
                ApplyVisualState(btn, bgColor, textColor);
            };

            // 🔥 Apply visual state immediately on first load
            ApplyVisualState(btn, bgColor, textColor);
        }

        private void ApplyVisualState(Button btn, Color bgColor, Color textColor)
        {
            if (btn.Enabled)
            {
                btn.BackColor = bgColor;
                btn.ForeColor = textColor;
                btn.Cursor = Cursors.Hand;
            }
            else
            {
                btn.BackColor = Color.LightGray;
                btn.ForeColor = Color.DarkGray;
                btn.Cursor = Cursors.Default;
            }
        }
        private void AddImagePreviewPanel(Control parent, int padding)
        {
            panelImagePreview.Visible = true;
            panelImagePreview.Location = new Point(padding, imageScroll.Bottom + 10);
            panelImagePreview.Size = new Size(parent.Width - 2 * padding, parent.Height - (imageScroll.Bottom + 20));
            panelImagePreview.BorderStyle = BorderStyle.FixedSingle;
            panelImagePreview.BackColor = Color.WhiteSmoke;
            panelImagePreview.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            panelImagePreview.Controls.Clear();

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55F)); // Left image
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 8F));  // Nav buttons
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 37)); // Right image

            // === LEFT: Match Image Panel ===
            var leftPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            layout.Controls.Add(leftPanel, 0, 0);
            // === RIGHT: Reference Image Preview ===
            picMainImage.Dock = DockStyle.Fill;
            picMainImage.SizeMode = PictureBoxSizeMode.Zoom;
            layout.Controls.Add(picMainImage, 2, 0);

            panelImagePreview.Controls.Add(layout);
            parent.Controls.Add(panelImagePreview);

            // Save reference for loading match frames later
            panelImagePreview.Tag = leftPanel;
        }

        private void AddSuspectDetailsPanel(dynamic suspect)
        {
            int padding = 20;
            var detailsPanel = new Panel
            {
                Width = 260,
                Height = panelDetails.Height - 2 * padding,
                Location = new Point(panelDetails.Width - 260, padding),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                AutoScroll = true,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.WhiteSmoke
            };

            int y = 10;
            void AddDetail(string label)
            {
                var lbl = new Label
                {
                    Text = label,
                    AutoSize = true,
                    Location = new Point(10, y),
                    Font = new Font("Segoe UI", 9)
                };
                detailsPanel.Controls.Add(lbl);
                y += lbl.Height + 8;
            }

            AddDetail($"DOB: {suspect.Dob}");
            AddDetail($"FIR No: {suspect.FirNo}");
            AddDetail($"Created At: {suspect.CreatedAt}");
            AddDetail($"Updated At: {suspect.UpdatedAt}");

            panelDetails.Controls.Add(detailsPanel);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var rect = new Rectangle(0, 0, this.Width - 1, this.Height - 1);
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddArc(rect.X, rect.Y, 10, 10, 180, 90);
            path.AddArc(rect.Right - 10, rect.Y, 10, 10, 270, 90);
            path.AddArc(rect.Right - 10, rect.Bottom - 10, 10, 10, 0, 90);
            path.AddArc(rect.X, rect.Bottom - 10, 10, 10, 90, 90);
            path.CloseFigure();
            this.Region = new Region(path);
        }



        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            string keyword = txtSearch.Text.Trim().ToLower();
            var filtered = allSuspects
                .Where(s => s.Name.ToLower().Contains(keyword))
                .ToList();

            DisplaySuspects(filtered);
        }
        private void btnEnroll_Click(object sender, EventArgs e)
        {
            var form = new EnrollSuspectForm(); // new suspect
            if (form.ShowDialog() == DialogResult.OK)
            {
                DbHelper.InsertOrUpdateSuspect(form.Suspect);
                LoadSuspectCards();
            }
        }

        private async Task ReloadSingleSuspect(int suspectId)
        {
            try
            {
                var result = DbHelper.GetSuspectById(suspectId);
                if (result == null)
                {
                    //_logger.LogWarning("Suspect ID {0} not found.", suspectId);
                    return;
                }

                var (name, blobs) = result.Value;
                var singleSuspect = new Dictionary<int, (string name, List<byte[]> blobs)>
                {
                    [suspectId] = (name, blobs)
                };

                var matcher = new SuspectMatcher();
                var newEmbeddings = await Task.Run(() =>
                    matcher.PrecomputeSuspectEmbeddingsFromBlobs(singleSuspect, msg => PrintLog()));

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
                }
            }
            catch (Exception ex)
            {

            }
        }
        public void PrintLog()
        {

        }

    }
    internal static class NativeMethods
    {
        [System.Runtime.InteropServices.DllImport("gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        public static extern IntPtr CreateRoundRectRgn(
            int nLeftRect, int nTopRect,
            int nRightRect, int nBottomRect,
            int nWidthEllipse, int nHeightEllipse);
    }
}