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
            label1 = new Label();
            txtSuspectDir = new TextBox();
            txtImageDir = new TextBox();
            label2 = new Label();
            txtResultDir = new TextBox();
            label3 = new Label();
            button1 = new Button();
            txtLog = new RichTextBox();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(63, 39);
            label1.Name = "label1";
            label1.Size = new Size(95, 20);
            label1.TabIndex = 0;
            label1.Text = "Suspect Path:";
            // 
            // txtSuspectDir
            // 
            txtSuspectDir.Location = new Point(196, 32);
            txtSuspectDir.Name = "txtSuspectDir";
            txtSuspectDir.Size = new Size(555, 27);
            txtSuspectDir.TabIndex = 1;
            // 
            // txtImageDir
            // 
            txtImageDir.Location = new Point(196, 91);
            txtImageDir.Name = "txtImageDir";
            txtImageDir.Size = new Size(555, 27);
            txtImageDir.TabIndex = 3;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(63, 98);
            label2.Name = "label2";
            label2.Size = new Size(85, 20);
            label2.TabIndex = 2;
            label2.Text = "Frame Path:";
            // 
            // txtResultDir
            // 
            txtResultDir.Location = new Point(196, 151);
            txtResultDir.Name = "txtResultDir";
            txtResultDir.Size = new Size(555, 27);
            txtResultDir.TabIndex = 5;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(63, 158);
            label3.Name = "label3";
            label3.RightToLeft = RightToLeft.No;
            label3.Size = new Size(84, 20);
            label3.TabIndex = 4;
            label3.Text = "Result Path:";
            // 
            // button1
            // 
            button1.Location = new Point(196, 211);
            button1.Name = "button1";
            button1.Size = new Size(125, 29);
            button1.TabIndex = 6;
            button1.Text = "Start Matcher";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // txtLog
            // 
            txtLog.Location = new Point(200, 270);
            txtLog.Name = "txtLog";
            txtLog.Size = new Size(551, 217);
            txtLog.TabIndex = 7;
            txtLog.Text = "";
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 499);
            Controls.Add(txtLog);
            Controls.Add(button1);
            Controls.Add(txtResultDir);
            Controls.Add(label3);
            Controls.Add(txtImageDir);
            Controls.Add(label2);
            Controls.Add(txtSuspectDir);
            Controls.Add(label1);
            Name = "Form1";
            Text = "Face Matcher Schedular";
            Load += Form1_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label label1;
        private TextBox txtSuspectDir;
        private TextBox txtImageDir;
        private Label label2;
        private TextBox txtResultDir;
        private Label label3;
        private Button button1;
        private RichTextBox txtLog;
    }
}
