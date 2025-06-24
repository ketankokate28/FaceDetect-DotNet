using System;
using System.Drawing;
using System.Windows.Forms;

namespace UI.Controls
{
    public partial class SuspectCard : UserControl
    {
        public int SuspectId { get; set; }
        public event EventHandler CardClicked;

        public SuspectCard()
        {
            InitializeComponent();
            this.Click += (s, e) => CardClicked?.Invoke(this, EventArgs.Empty);
            foreach (Control ctl in this.Controls)
            {
                ctl.Click += (s, e) => CardClicked?.Invoke(this, EventArgs.Empty);
            }
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