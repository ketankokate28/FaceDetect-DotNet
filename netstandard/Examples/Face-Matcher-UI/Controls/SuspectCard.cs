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
        public SuspectCard()
        {
            InitializeComponent();
            this.Click += OnCardClicked;
            pictureBox.Click += OnCardClicked;
            lblName.Click += OnCardClicked;

            // editLabel.Click += (s, e) => EditClicked?.Invoke(this, EventArgs.Empty);
            editLabel.Click -= OnEditClicked; 
            editLabel.Click += OnEditClicked; 
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