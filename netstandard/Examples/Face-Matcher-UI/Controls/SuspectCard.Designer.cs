namespace UI.Controls
{
    partial class SuspectCard
    {
        private Label lblName;
        private PictureBox pictureBox;
        private PictureBox editIcon;

       // public event EventHandler EditClicked;
        private Label editLabel;
        private void InitializeComponent()
        {
            this.lblName = new Label();
            this.pictureBox = new PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox)).BeginInit();
            this.SuspendLayout();

            // pictureBox
            this.pictureBox.SizeMode = PictureBoxSizeMode.Zoom;
            this.pictureBox.Dock = DockStyle.Top;
            this.pictureBox.Height = 120;
            this.pictureBox.Margin = new Padding(0);

            // lblName
            this.lblName.Dock = DockStyle.Fill;
            this.lblName.TextAlign = ContentAlignment.MiddleCenter;
            this.lblName.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            this.lblName.Margin = new Padding(0);

            // SuspectCard (main control)
            this.Controls.Add(this.lblName);
            this.Controls.Add(this.pictureBox);
            this.Size = new Size(150, 160);
            this.Margin = new Padding(10);
            this.BackColor = Color.White;
            this.BorderStyle = BorderStyle.FixedSingle;

            // Common click for whole card
            this.Click += SuspectCard_Click;
            this.pictureBox.Click += SuspectCard_Click;
            this.lblName.Click += SuspectCard_Click;

            // Edit Icon
            editLabel = new Label
            {
                Text = "✏️",  // Unicode pencil
                AutoSize = true,
                Font = new Font("Segoe UI", 9),
                Location = new Point(this.Width - 22, 5),
                Cursor = Cursors.Hand,
                BackColor = Color.Transparent
            };

            // Stop event from bubbling up to CardClicked
            //editLabel.Click += (s, e) =>
            //{
            //    e = e as MouseEventArgs;
            //    EditClicked?.Invoke(this, EventArgs.Empty);
            //};

            // Attach a mouse down to stop further propagation if needed
          //  editLabel.MouseDown += (s, e) => e.Handled = true;

            this.Controls.Add(editLabel);
            editLabel.BringToFront();

            ((System.ComponentModel.ISupportInitialize)(this.pictureBox)).EndInit();
            this.ResumeLayout(false);
        }


    }
}