using Model;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace SuspectManager.Forms
{
    public partial class EnrollSuspectForm : Form
    {
        // 🔁 Store base64 instead of paths
        private List<string> imageBase64s = new();
        public Suspect Suspect { get; private set; }

        public EnrollSuspectForm(Suspect existing = null)
        {
            InitializeComponent();
            Suspect = existing ?? new Suspect();
            BindData();
        }

        private void BindData()
        {
            txtFirstName.Text = Suspect.FirstName;
            txtLastName.Text = Suspect.LastName;
            cmbGender.SelectedItem = Suspect.Gender;
            txtNationality.Text = Suspect.FirNo;
            dtpDob.Value = DateTime.TryParse(Suspect.Dob, out var d) ? d : DateTime.Now;

            for (int i = 0; i < Suspect.Images.Length; i++)
            {
                var base64 = Suspect.Images[i];
                if (!string.IsNullOrWhiteSpace(base64))
                {
                    while (imageBase64s.Count <= i) imageBase64s.Add("");
                    imageBase64s[i] = base64;

                    try
                    {
                        var imageBytes = Convert.FromBase64String(base64);
                        using var ms = new MemoryStream(imageBytes);
                        picImages[i].Image = Image.FromStream(ms);
                        btnRemove[i].Visible = true;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error loading image: {ex.Message}");
                    }
                }
            }
        }

        private void BrowseImage_Click(object sender, EventArgs e)
        {
            var index = (int)((Button)sender).Tag;

            using var dialog = new OpenFileDialog
            {
                Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp"
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                var selectedPath = dialog.FileName;

                try
                {
                    using var img = Image.FromFile(selectedPath);
                    picImages[index].Image = new Bitmap(img);
                    btnRemove[index].Visible = true;

                    // 🔁 Convert to base64
                    using var ms = new MemoryStream();
                    img.Save(ms, img.RawFormat);
                    string base64 = Convert.ToBase64String(ms.ToArray());

                    while (imageBase64s.Count <= index)
                        imageBase64s.Add("");

                    imageBase64s[index] = base64;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading selected image: {ex.Message}");
                }
            }
        }

        private void RemoveImage_Click(object sender, EventArgs e)
        {
            int index = (int)((Button)sender).Tag;
            if (index < picImages.Length)
            {
                picImages[index].Image = null;
                if (imageBase64s.Count > index)
                    imageBase64s[index] = "";

                btnRemove[index].Visible = false;
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtFirstName.Text))
            {
                MessageBox.Show("First name is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int imageCount = 0;
            foreach (var b64 in imageBase64s)
            {
                if (!string.IsNullOrWhiteSpace(b64))
                    imageCount++;
            }

            if (imageCount < 1)
            {
                MessageBox.Show("At least one image is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Suspect.FirstName = txtFirstName.Text.Trim();
            Suspect.LastName = txtLastName.Text.Trim();
            Suspect.Gender = cmbGender.SelectedItem?.ToString() ?? "";
            Suspect.Dob = dtpDob.Value.ToString("yyyy-MM-dd");
            Suspect.FirNo = txtNationality.Text.Trim();
            Suspect.UpdatedAt = DateTime.Now.ToString("s");

            for (int i = 0; i < 10; i++)
                Suspect.Images[i] = i < imageBase64s.Count ? imageBase64s[i] ?? "" : "";

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
