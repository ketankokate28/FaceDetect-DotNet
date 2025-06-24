using System;
using System.Drawing;
using System.Windows.Forms;
using UI.Controls;
using System.Collections.Generic;
using System.Linq;
using Face_Matcher_UI;
using SuspectManager.Forms;

namespace UI
{
    public partial class SuspectListControl : UserControl
    {
        private Panel panelDetails;
        private List<(int Id, string Name, Image Img)> allSuspects;
        public SuspectListControl()
        {
            InitializeComponent();


            LoadSuspectCards();
            splitContainer.SplitterDistance = splitContainer.SplitterDistance + 1;
            splitContainer.SplitterDistance = splitContainer.SplitterDistance - 1;
            splitContainer.BorderStyle = BorderStyle.Fixed3D;
            splitContainer.Panel2MinSize = 1;
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
                card.SuspectId = s.Id;
                card.SetData(s.Name, s.Img);
                card.CardClicked += (sender, e) =>
                {
                    ShowSuspectDetails(s.Id);
                };
                flowLayoutPanelSuspects.Controls.Add(card);
            }
        }
        private void ShowSuspectDetails(int suspectId)
        {
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
            // Name + Gender (top-left)
            var nameGenderLabel = new Label
            {
            Text = $"{suspect.FirstName} {suspect.LastName} ({genderDisplay})",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(padding, padding)
            };
            panelDetails.Controls.Add(nameGenderLabel);

            // Horizontal image scroll (below name)
            var imageScroll = new FlowLayoutPanel
            {
                Location = new Point(padding, nameGenderLabel.Bottom + 10),
                Size = new Size(panelDetails.Width - 300, 110),
                AutoScroll = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BorderStyle = BorderStyle.FixedSingle
            };

            foreach (string b64 in suspect.Images)
            {
                if (!string.IsNullOrWhiteSpace(b64))
                {
                    try
                    {
                        byte[] imgBytes = Convert.FromBase64String(b64);
                        using var ms = new System.IO.MemoryStream(imgBytes);
                        var pic = new PictureBox
                        {
                            Image = Image.FromStream(ms),
                            SizeMode = PictureBoxSizeMode.Zoom,
                            Size = new Size(100, 100),
                            Margin = new Padding(5)
                        };
                        imageScroll.Controls.Add(pic);
                    }
                    catch { }
                }
            }

            panelDetails.Controls.Add(imageScroll);

            // Far right details (like a sidebar)
            var detailsPanel = new Panel
            {
                Size = new Size(260, 130),
                Location = new Point(panelDetails.Width - 260, padding),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };

            int y = 0;
            void AddDetail(string label)
            {
                var lbl = new Label
                {
                    Text = label,
                    AutoSize = true,
                    Location = new Point(0, y),
                    Font = new Font("Segoe UI", 9)
                };
                detailsPanel.Controls.Add(lbl);
                y += lbl.Height + 5;
            }

            AddDetail($"DOB: {suspect.Dob}");
            AddDetail($"FIR No: {suspect.FirNo}");
            AddDetail($"Created At: {suspect.CreatedAt}");
            AddDetail($"Updated At: {suspect.UpdatedAt}");

            panelDetails.Controls.Add(detailsPanel);

            // Optional separator
            var separator = new Panel
            {
                Size = new Size(1, 130),
                Location = new Point(detailsPanel.Left - 10, padding),
                BackColor = Color.LightGray
            };
            panelDetails.Controls.Add(separator);
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
    }
}