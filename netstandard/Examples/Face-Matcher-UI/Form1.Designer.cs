namespace Face_Matcher_UI
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            button1 = new Button();
            txtLog = new RichTextBox();
            btnBrowseSuspect = new Button();
            openFileDialog1 = new OpenFileDialog();
            folderBrowserDialog1 = new FolderBrowserDialog();
            comboBox1 = new ComboBox();
            comboBox2 = new ComboBox();
            button2 = new Button();
            pictureBox1 = new PictureBox();
            btnPrevious = new Button();
            btnNext = new Button();
            label1 = new Label();
            menuStrip1 = new MenuStrip();
            fileToolStripMenuItem = new ToolStripMenuItem();
            //enrollSuspectToolStripMenuItem = new ToolStripMenuItem();
            //addImagesToolStripMenuItem = new ToolStripMenuItem();
            videoCutterToolStripMenuItem = new ToolStripMenuItem();
            videoMatcherToolStripMenuItem = new ToolStripMenuItem();
            exitToolStripMenuItem = new ToolStripMenuItem();
            panel1 = new Panel();
            contentPanel = new Panel();
            homeToolStripMenuItem = new ToolStripMenuItem();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            menuStrip1.SuspendLayout();
            contentPanel.SuspendLayout();
            SuspendLayout();
            // 
            // button1
            // 
            button1.Location = new Point(730, 49);
            button1.Name = "button1";
            button1.Size = new Size(125, 29);
            button1.TabIndex = 6;
            button1.Text = "Start Matcher";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // txtLog
            // 
            txtLog.Location = new Point(1056, 35);
            txtLog.Name = "txtLog";
            txtLog.Size = new Size(266, 553);
            txtLog.TabIndex = 7;
            txtLog.Text = "";
            // 
            // btnBrowseSuspect
            // 
            btnBrowseSuspect.Location = new Point(169, 48);
            btnBrowseSuspect.Name = "btnBrowseSuspect";
            btnBrowseSuspect.Size = new Size(158, 29);
            btnBrowseSuspect.TabIndex = 8;
            btnBrowseSuspect.Text = "Enroll Suspect";
            btnBrowseSuspect.UseVisualStyleBackColor = true;
            btnBrowseSuspect.Click += btnBrowseSuspect_Click;
            // 
            // openFileDialog1
            // 
            openFileDialog1.FileName = "openFileDialog1";
            // 
            // comboBox1
            // 
            comboBox1.FormattingEnabled = true;
            comboBox1.Items.AddRange(new object[] { "File", "Directory" });
            comboBox1.Location = new Point(12, 48);
            comboBox1.Name = "comboBox1";
            comboBox1.Size = new Size(151, 28);
            comboBox1.TabIndex = 9;
            // 
            // comboBox2
            // 
            comboBox2.FormattingEnabled = true;
            comboBox2.Items.AddRange(new object[] { "File", "Directory" });
            comboBox2.Location = new Point(398, 48);
            comboBox2.Name = "comboBox2";
            comboBox2.Size = new Size(151, 28);
            comboBox2.TabIndex = 11;
            // 
            // button2
            // 
            button2.Location = new Point(555, 48);
            button2.Name = "button2";
            button2.Size = new Size(158, 29);
            button2.TabIndex = 10;
            button2.Text = "Images Path";
            button2.UseVisualStyleBackColor = true;
            button2.Click += button2_Click;
            // 
            // pictureBox1
            // 
            pictureBox1.Location = new Point(0, 28);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(1321, 676);
            pictureBox1.TabIndex = 12;
            pictureBox1.TabStop = false;
            // 
            // btnPrevious
            // 
            btnPrevious.Location = new Point(129, 594);
            btnPrevious.Name = "btnPrevious";
            btnPrevious.Size = new Size(94, 29);
            btnPrevious.TabIndex = 13;
            btnPrevious.Text = "Previous <";
            btnPrevious.UseVisualStyleBackColor = true;
            btnPrevious.Click += btnPrevious_Click_1;
            // 
            // btnNext
            // 
            btnNext.Location = new Point(278, 594);
            btnNext.Name = "btnNext";
            btnNext.Size = new Size(94, 29);
            btnNext.TabIndex = 14;
            btnNext.Text = "Next >";
            btnNext.UseVisualStyleBackColor = true;
            btnNext.Click += btnNext_Click_1;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(198, 626);
            label1.Name = "label1";
            label1.Size = new Size(0, 20);
            label1.TabIndex = 15;
            // 
            // menuStrip1
            // 
            menuStrip1.ImageScalingSize = new Size(20, 20);
            menuStrip1.Items.AddRange(new ToolStripItem[] { fileToolStripMenuItem, homeToolStripMenuItem, videoMatcherToolStripMenuItem, videoCutterToolStripMenuItem, exitToolStripMenuItem });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new Size(1321, 28);
            menuStrip1.TabIndex = 16;
            menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            fileToolStripMenuItem.Size = new Size(46, 24);
            fileToolStripMenuItem.Text = "File";
            // 
            // enrollSuspectToolStripMenuItem
            // 
            //enrollSuspectToolStripMenuItem.Name = "enrollSuspectToolStripMenuItem";
            //enrollSuspectToolStripMenuItem.Size = new Size(116, 24);
            //enrollSuspectToolStripMenuItem.Text = "Enroll Suspect";
            //enrollSuspectToolStripMenuItem.Click += enrollSuspectToolStripMenuItem_Click;
            // 
            // addImagesToolStripMenuItem
            // 
            //addImagesToolStripMenuItem.Name = "addImagesToolStripMenuItem";
            //addImagesToolStripMenuItem.Size = new Size(103, 24);
            //addImagesToolStripMenuItem.Text = "Add Images";
            //addImagesToolStripMenuItem.Click += addImagesToolStripMenuItem_Click;
            // 
            // videoCutterToolStripMenuItem
            // 
            videoCutterToolStripMenuItem.Name = "videoCutterToolStripMenuItem";
            videoCutterToolStripMenuItem.Size = new Size(106, 24);
            videoCutterToolStripMenuItem.Text = "Video Cutter";
            videoCutterToolStripMenuItem.Click += videoCutterToolStripMenuItem_Click;
            // 
            // videoMatcherToolStripMenuItem
            // 
            videoMatcherToolStripMenuItem.Name = "videoMatcherToolStripMenuItem";
            videoMatcherToolStripMenuItem.Size = new Size(120, 24);
            videoMatcherToolStripMenuItem.Text = "Quick Match";
            videoMatcherToolStripMenuItem.Click += videoMatcherToolStripMenuItem_Click;
            // 
            // exitToolStripMenuItem
            // 
            exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            exitToolStripMenuItem.Size = new Size(47, 24);
            exitToolStripMenuItem.Text = "Exit";
            exitToolStripMenuItem.Click += exitToolStripMenuItem_Click;
            // 
            // panel1
            // 
            panel1.BackColor = Color.Gray;
            panel1.Dock = DockStyle.Top;
            panel1.Location = new Point(0, 28);
            panel1.Name = "panel1";
            panel1.Size = new Size(1321, 1);
            panel1.TabIndex = 17;
            // 
            // contentPanel
            // 
            contentPanel.Controls.Add(label1);
            contentPanel.Controls.Add(btnNext);
            contentPanel.Controls.Add(btnPrevious);
            contentPanel.Controls.Add(pictureBox1);
            contentPanel.Controls.Add(comboBox2);
            contentPanel.Controls.Add(button2);
            contentPanel.Controls.Add(comboBox1);
            contentPanel.Controls.Add(btnBrowseSuspect);
            contentPanel.Controls.Add(txtLog);
            contentPanel.Controls.Add(button1);
            contentPanel.Dock = DockStyle.Fill;
            contentPanel.Location = new Point(0, 29);
            contentPanel.Name = "contentPanel";
            contentPanel.Size = new Size(1321, 675);
            contentPanel.TabIndex = 18;
            // 
            // homeToolStripMenuItem
            // 
            homeToolStripMenuItem.Name = "homeToolStripMenuItem";
            homeToolStripMenuItem.Size = new Size(64, 24);
            homeToolStripMenuItem.Text = "Home";
            homeToolStripMenuItem.Click += homeToolStripMenuItem_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1321, 704);
            Controls.Add(contentPanel);
            Controls.Add(panel1);
            Controls.Add(menuStrip1);
            Name = "Form1";
            Text = "Face Matcher Schedular";
            Load += Form1_Load;
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            contentPanel.ResumeLayout(false);
            contentPanel.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private Button button1;
        private RichTextBox txtLog;
        private Button btnBrowseSuspect;
        private OpenFileDialog openFileDialog1;
        private FolderBrowserDialog folderBrowserDialog1;
        private ComboBox comboBox1;
        private ComboBox comboBox2;
        private Button button2;
        private PictureBox pictureBox1;
        private Button btnPrevious;
        private Button btnNext;
        private Label labelImageCounter;
        private Label label1;
        private MenuStrip menuStrip1;
        private ToolStripMenuItem fileToolStripMenuItem;
        //private ToolStripMenuItem enrollSuspectToolStripMenuItem;
        //private ToolStripMenuItem addImagesToolStripMenuItem;
        private ToolStripMenuItem exitToolStripMenuItem;
        private Panel panel1;
        private ToolStripMenuItem videoCutterToolStripMenuItem;
        private ToolStripMenuItem videoMatcherToolStripMenuItem;
        private Panel contentPanel;
        private ToolStripMenuItem homeToolStripMenuItem;
    }
}
