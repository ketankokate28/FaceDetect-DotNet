using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace Face_Matcher_UI
{
    public partial class StopProgressForm : Form
    {
        private System.Windows.Forms.Timer countdownTimer;
        private int secondsLeft = 5;

        public StopProgressForm()
        {
            InitializeComponent();
            InitializeCustomComponents();
        }

        private void InitializeCustomComponents()
        {
            this.Text = "Stopping";
            this.Size = new Size(300, 120);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.ControlBox = false;
            this.StartPosition = FormStartPosition.CenterParent;

            Label lbl = new Label
            {
                Text = "Stopping search, please wait...",
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 30
            };
            this.Controls.Add(lbl);

            ProgressBar progressBar = new ProgressBar
            {
                Dock = DockStyle.Top,
                Style = ProgressBarStyle.Continuous,
                Minimum = 0,
                Maximum = 5,
                Value = 0,
                Height = 20
            };
            this.Controls.Add(progressBar);

            countdownTimer = new System.Windows.Forms.Timer();
            countdownTimer.Interval = 1000;
            countdownTimer.Tick += (s, e) =>
            {
                secondsLeft--;
                progressBar.Value = 5 - secondsLeft;

                if (secondsLeft <= 0)
                {
                    countdownTimer.Stop();
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
            };

            countdownTimer.Start();
        }
    }

}
