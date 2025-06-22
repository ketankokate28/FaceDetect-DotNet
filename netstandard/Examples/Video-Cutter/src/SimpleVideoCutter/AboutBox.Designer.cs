namespace SimpleVideoCutter
{
    partial class AboutBox
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AboutBox));
            tableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            logoPictureBox = new System.Windows.Forms.PictureBox();
            labelProductName = new System.Windows.Forms.Label();
            labelVersion = new System.Windows.Forms.Label();
            labelCopyright = new System.Windows.Forms.Label();
            textBox1 = new System.Windows.Forms.TextBox();
            linkLabelGithub = new System.Windows.Forms.LinkLabel();
            linkLabelGithubReleases = new System.Windows.Forms.LinkLabel();
            linkLabelEmail = new System.Windows.Forms.LinkLabel();
            panel1 = new System.Windows.Forms.Panel();
            buttonLicense = new System.Windows.Forms.Button();
            okButton = new System.Windows.Forms.Button();
            tableLayoutPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)logoPictureBox).BeginInit();
            panel1.SuspendLayout();
            SuspendLayout();
            // 
            // tableLayoutPanel
            // 
            resources.ApplyResources(tableLayoutPanel, "tableLayoutPanel");
            tableLayoutPanel.Controls.Add(logoPictureBox, 0, 0);
            tableLayoutPanel.Controls.Add(labelProductName, 1, 0);
            tableLayoutPanel.Controls.Add(labelVersion, 1, 1);
            tableLayoutPanel.Controls.Add(labelCopyright, 1, 2);
            tableLayoutPanel.Controls.Add(textBox1, 1, 6);
            tableLayoutPanel.Controls.Add(linkLabelGithub, 1, 4);
            tableLayoutPanel.Controls.Add(linkLabelGithubReleases, 1, 3);
            tableLayoutPanel.Controls.Add(linkLabelEmail, 1, 5);
            tableLayoutPanel.Controls.Add(panel1, 1, 7);
            tableLayoutPanel.Name = "tableLayoutPanel";
            // 
            // logoPictureBox
            // 
            resources.ApplyResources(logoPictureBox, "logoPictureBox");
            logoPictureBox.Image = Properties.Resources.film_reels;
            logoPictureBox.Name = "logoPictureBox";
            tableLayoutPanel.SetRowSpan(logoPictureBox, 8);
            logoPictureBox.TabStop = false;
            // 
            // labelProductName
            // 
            resources.ApplyResources(labelProductName, "labelProductName");
            labelProductName.Name = "labelProductName";
            // 
            // labelVersion
            // 
            resources.ApplyResources(labelVersion, "labelVersion");
            labelVersion.Name = "labelVersion";
            // 
            // labelCopyright
            // 
            resources.ApplyResources(labelCopyright, "labelCopyright");
            labelCopyright.Name = "labelCopyright";
            // 
            // textBox1
            // 
            textBox1.BackColor = System.Drawing.SystemColors.Control;
            textBox1.BorderStyle = System.Windows.Forms.BorderStyle.None;
            resources.ApplyResources(textBox1, "textBox1");
            textBox1.Name = "textBox1";
            textBox1.TabStop = false;
            // 
            // linkLabelGithub
            // 
            resources.ApplyResources(linkLabelGithub, "linkLabelGithub");
            linkLabelGithub.Name = "linkLabelGithub";
            linkLabelGithub.TabStop = true;
            linkLabelGithub.LinkClicked += linkLabelGithub_LinkClicked;
            // 
            // linkLabelGithubReleases
            // 
            resources.ApplyResources(linkLabelGithubReleases, "linkLabelGithubReleases");
            linkLabelGithubReleases.Name = "linkLabelGithubReleases";
            linkLabelGithubReleases.TabStop = true;
            linkLabelGithubReleases.LinkClicked += linkLabelGithubreleases_LinkClicked;
            // 
            // linkLabelEmail
            // 
            resources.ApplyResources(linkLabelEmail, "linkLabelEmail");
            linkLabelEmail.Name = "linkLabelEmail";
            linkLabelEmail.TabStop = true;
            linkLabelEmail.LinkClicked += linkLabelEmail_LinkClicked;
            // 
            // panel1
            // 
            panel1.Controls.Add(buttonLicense);
            panel1.Controls.Add(okButton);
            resources.ApplyResources(panel1, "panel1");
            panel1.Name = "panel1";
            // 
            // buttonLicense
            // 
            resources.ApplyResources(buttonLicense, "buttonLicense");
            buttonLicense.Name = "buttonLicense";
            buttonLicense.Click += buttonLicense_Click;
            // 
            // okButton
            // 
            resources.ApplyResources(okButton, "okButton");
            okButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            okButton.Name = "okButton";
            // 
            // AboutBox
            // 
            AcceptButton = okButton;
            resources.ApplyResources(this, "$this");
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            CancelButton = okButton;
            Controls.Add(tableLayoutPanel);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "AboutBox";
            ShowIcon = false;
            ShowInTaskbar = false;
            Load += AboutBox_Load;
            tableLayoutPanel.ResumeLayout(false);
            tableLayoutPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)logoPictureBox).EndInit();
            panel1.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel;
        private System.Windows.Forms.PictureBox logoPictureBox;
        private System.Windows.Forms.Label labelProductName;
        private System.Windows.Forms.Label labelCopyright;
        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.Label labelVersion;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.LinkLabel linkLabelGithub;
        private System.Windows.Forms.LinkLabel linkLabelGithubReleases;
        private System.Windows.Forms.LinkLabel linkLabelEmail;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button buttonLicense;
    }
}
