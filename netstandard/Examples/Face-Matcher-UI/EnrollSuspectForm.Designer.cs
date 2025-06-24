namespace SuspectManager.Forms
{
    partial class EnrollSuspectForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.TextBox txtFirstName;
        private System.Windows.Forms.TextBox txtLastName;
        private System.Windows.Forms.ComboBox cmbGender;
        private System.Windows.Forms.DateTimePicker dtpDob;
        private System.Windows.Forms.TextBox txtNationality;
        private System.Windows.Forms.Button btnSave;

        private System.Windows.Forms.PictureBox[] picImages = new System.Windows.Forms.PictureBox[10];
        private System.Windows.Forms.Button[] btnBrowse = new System.Windows.Forms.Button[10];
        private System.Windows.Forms.Button[] btnRemove = new System.Windows.Forms.Button[10];

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();

            txtFirstName = new TextBox();
            txtLastName = new TextBox();
            cmbGender = new ComboBox();
            dtpDob = new DateTimePicker();
            txtNationality = new TextBox();
            btnSave = new Button();

            SuspendLayout();

            // Basic fields
            txtFirstName.Location = new Point(20, 20);
            txtFirstName.PlaceholderText = "First Name";
            txtFirstName.Width = 250;

            txtLastName.Location = new Point(20, 60);
            txtLastName.PlaceholderText = "Last Name";
            txtLastName.Width = 250;

            cmbGender.Location = new Point(20, 100);
            cmbGender.Items.AddRange(new object[] { "M", "F", "O", "U" });
            cmbGender.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbGender.Width = 250;

            dtpDob.Location = new Point(20, 140);
            dtpDob.Format = DateTimePickerFormat.Short;
            dtpDob.Width = 250;

            txtNationality.Location = new Point(20, 180);
            txtNationality.PlaceholderText = "FIR/ADR No.";
            txtNationality.Width = 250;

            // Thumbnails and buttons
            for (int i = 0; i < 10; i++)
            {
                int col = i % 5;
                int row = i / 5;

                int baseX = 300 + col * 140;
                int baseY = 20 + row * 200;

                // Picture box
                picImages[i] = new PictureBox();
                picImages[i].Location = new Point(baseX, baseY);
                picImages[i].Size = new Size(120, 120);
                picImages[i].SizeMode = PictureBoxSizeMode.Zoom;
                picImages[i].BorderStyle = BorderStyle.FixedSingle;

                // Browse button
                btnBrowse[i] = new Button();
                btnBrowse[i].Location = new Point(baseX, baseY + 125);
                btnBrowse[i].Size = new Size(58, 30);
                btnBrowse[i].Text = "Browse";
                btnBrowse[i].Tag = i;
                btnBrowse[i].Click += BrowseImage_Click;

                // Remove button
                btnRemove[i] = new Button();
                btnRemove[i].Size = new Size(20, 20);
                btnRemove[i].Location = new Point(baseX + 100, baseY - 2);
                btnRemove[i].Text = "X";
                btnRemove[i].Tag = i;
                btnRemove[i].BackColor = Color.Red;
                btnRemove[i].ForeColor = Color.White;
                btnRemove[i].FlatStyle = FlatStyle.Flat;
                btnRemove[i].FlatAppearance.BorderSize = 0;
                btnRemove[i].Click += RemoveImage_Click;
                btnRemove[i].Visible = false; // initially hidden

                // Add controls in proper order
                Controls.Add(picImages[i]);
                Controls.Add(btnBrowse[i]);
                Controls.Add(btnRemove[i]);

                // Force the remove button to front
                btnRemove[i].BringToFront();
            }


            // Save Button
            btnSave.Location = new Point(20, 240);
            btnSave.Size = new Size(250, 40);
            btnSave.Text = "Save";
            btnSave.Click += btnSave_Click;

            Controls.Add(txtFirstName);
            Controls.Add(txtLastName);
            Controls.Add(cmbGender);
            Controls.Add(dtpDob);
            Controls.Add(txtNationality);
            Controls.Add(btnSave);

            // Final form settings
            Text = "Enroll / Edit Suspect";
            ClientSize = new Size(1050, 440 + 200); // taller form to fit all
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;

            ResumeLayout(false);
            PerformLayout();
        }

    }
}
