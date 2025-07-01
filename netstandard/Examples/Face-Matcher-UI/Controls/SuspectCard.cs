using System;
using System.Drawing;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace UI.Controls
{
    public partial class SuspectCard : UserControl
    {
        public int SuspectId { get; set; }
        public event EventHandler CardClicked;
        public event EventHandler EditClicked;
        private bool _editRecentlyClicked = false;
        public event EventHandler DeleteClicked;
        private Label deleteLabel;

        public SuspectCard()
        {
            InitializeComponent();
            this.Click += OnCardClicked;
            pictureBox.Click += OnCardClicked;
            lblName.Click += OnCardClicked;

            // editLabel.Click += (s, e) => EditClicked?.Invoke(this, EventArgs.Empty);
            editLabel.Click -= OnEditClicked; 
            editLabel.Click += OnEditClicked;
            deleteLabel = new Label
            {
                Text = "🗑",  // Trash icon
                AutoSize = true,
                Font = new Font("Segoe UI", 10),
                Location = new Point(5, 5), // Top-left corner
                Cursor = Cursors.Hand,
                ForeColor = Color.DarkRed,
                BackColor = Color.Transparent
            };
            deleteLabel.Click += OnDeleteClicked;

            this.Controls.Add(deleteLabel);
            deleteLabel.BringToFront();

        }
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (editLabel != null)
                editLabel.Location = new Point(this.Width - 22, 5);
        }
        private void OnDeleteClicked(object sender, EventArgs e)
        {
            DeleteClicked?.Invoke(this, EventArgs.Empty);
        }
        public void ClearDeleteClickHandlers()
        {
            if (DeleteClicked != null)
            {
                foreach (Delegate d in DeleteClicked.GetInvocationList())
                {
                    DeleteClicked -= (EventHandler)d;
                }
            }
        }
        private void OnEditClicked(object sender, EventArgs e)
        {
            if (_editRecentlyClicked) return;

            _editRecentlyClicked = true;
            EditClicked?.Invoke(this, EventArgs.Empty);

            // Delay resetting to allow event to settle
            Timer timer = new Timer();
            timer.Interval = 500;
            timer.Tick += (s, args) =>
            {
                _editRecentlyClicked = false;
                timer.Stop();
                timer.Dispose();
            };
            timer.Start();
        }
        public void ClearEditClickHandlers()
        {
            if (EditClicked != null)
            {
                foreach (Delegate d in EditClicked.GetInvocationList())
                {
                    EditClicked -= (EventHandler)d;
                }
            }
        }
        public void DetachAllEvents()
        {
            if (CardClicked != null)
            {
                foreach (Delegate d in CardClicked.GetInvocationList())
                    CardClicked -= (EventHandler)d;
            }

            if (EditClicked != null)
            {
                foreach (Delegate d in EditClicked.GetInvocationList())
                    EditClicked -= (EventHandler)d;
            }
        }
        private void OnCardClicked(object sender, EventArgs e)
        {
            // Only fire if it’s not the edit label
            if (sender != editLabel)
                CardClicked?.Invoke(this, EventArgs.Empty);
        }
        public void SetData(string fullName, Image photo)
        {
            lblName.Text = fullName;
            pictureBox.Image = photo;
        }

        private void SuspectCard_Click(object sender, EventArgs e)
        {
            //MessageBox.Show($"Card clicked: {lblName.Text}, ID: {SuspectId}");
        }
    }
}