namespace Face_Matcher_UI
{
    partial class SettingsForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.StartPosition = FormStartPosition.CenterScreen;
            this.lblVideoToolPath = new System.Windows.Forms.Label();
            this.txtVideoToolPath = new System.Windows.Forms.TextBox();
            this.btnBrowse = new System.Windows.Forms.Button();
            this.lblFrameRate = new System.Windows.Forms.Label();
            this.cmbFrameRate = new System.Windows.Forms.ComboBox();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();

            this.SuspendLayout();

            // 
            // lblVideoToolPath
            // 
            this.lblVideoToolPath.AutoSize = true;
            this.lblVideoToolPath.Location = new System.Drawing.Point(12, 15);
            this.lblVideoToolPath.Name = "lblVideoToolPath";
            this.lblVideoToolPath.Size = new System.Drawing.Size(111, 20);
            this.lblVideoToolPath.TabIndex = 0;
            this.lblVideoToolPath.Text = "Video Tool Path:";

            // 
            // txtVideoToolPath
            // 
            this.txtVideoToolPath.Location = new System.Drawing.Point(129, 12);
            this.txtVideoToolPath.Name = "txtVideoToolPath";
            this.txtVideoToolPath.Size = new System.Drawing.Size(300, 27);
            this.txtVideoToolPath.TabIndex = 1;

            // 
            // btnBrowse
            // 
            this.btnBrowse.Location = new System.Drawing.Point(435, 11);
            this.btnBrowse.Name = "btnBrowse";
            this.btnBrowse.Size = new System.Drawing.Size(75, 29);
            this.btnBrowse.TabIndex = 2;
            this.btnBrowse.Text = "...";
            this.btnBrowse.UseVisualStyleBackColor = true;
            this.btnBrowse.Click += new System.EventHandler(this.btnBrowse_Click);

            // 
            // lblFrameRate
            // 
            this.lblFrameRate.AutoSize = true;
            this.lblFrameRate.Location = new System.Drawing.Point(12, 56);
            this.lblFrameRate.Name = "lblFrameRate";
            this.lblFrameRate.Size = new System.Drawing.Size(86, 20);
            this.lblFrameRate.TabIndex = 3;
            this.lblFrameRate.Text = "Frame Rate:";

            // 
            // cmbFrameRate
            // 
            this.cmbFrameRate.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbFrameRate.FormattingEnabled = true;
            this.cmbFrameRate.Items.AddRange(new object[] { "1", "2", "3" });
            this.cmbFrameRate.Location = new System.Drawing.Point(129, 54);
            this.cmbFrameRate.Name = "cmbFrameRate";
            this.cmbFrameRate.Size = new System.Drawing.Size(66, 28);
            this.cmbFrameRate.TabIndex = 4;


            // 
            // lblStrictHigh
            // 
            this.lblStrictHigh = new System.Windows.Forms.Label();
            this.lblStrictHigh.AutoSize = true;
            this.lblStrictHigh.Location = new System.Drawing.Point(12, 90);
            this.lblStrictHigh.Name = "lblStrictHigh";
            this.lblStrictHigh.Size = new System.Drawing.Size(79, 20);
            this.lblStrictHigh.TabIndex = 7;
            this.lblStrictHigh.Text = "Strict High:";

            // 
            // txtStrictHigh
            // 
            this.txtStrictHigh = new System.Windows.Forms.TextBox();
            this.txtStrictHigh.Location = new System.Drawing.Point(129, 87);
            this.txtStrictHigh.Name = "txtStrictHigh";
            this.txtStrictHigh.Size = new System.Drawing.Size(66, 27);
            this.txtStrictHigh.TabIndex = 8;
            this.txtStrictHigh.Enabled = false;

            // 
            // lblStrictMedium
            // 
            this.lblStrictMedium = new System.Windows.Forms.Label();
            this.lblStrictMedium.AutoSize = true;
            this.lblStrictMedium.Location = new System.Drawing.Point(210, 90);
            this.lblStrictMedium.Name = "lblStrictMedium";
            this.lblStrictMedium.Size = new System.Drawing.Size(102, 20);
            this.lblStrictMedium.TabIndex = 9;
            this.lblStrictMedium.Text = "Strict Medium:";

            // 
            // txtStrictMedium
            // 
            this.txtStrictMedium = new System.Windows.Forms.TextBox();
            this.txtStrictMedium.Location = new System.Drawing.Point(318, 87);
            this.txtStrictMedium.Name = "txtStrictMedium";
            this.txtStrictMedium.Size = new System.Drawing.Size(66, 27);
            this.txtStrictMedium.TabIndex = 10;
            this.txtStrictMedium.Enabled = false;

            // 
            // lblStrictLow
            // 
            this.lblStrictLow = new System.Windows.Forms.Label();
            this.lblStrictLow.AutoSize = true;
            this.lblStrictLow.Location = new System.Drawing.Point(400, 90);
            this.lblStrictLow.Name = "lblStrictLow";
            this.lblStrictLow.Size = new System.Drawing.Size(76, 20);
            this.lblStrictLow.TabIndex = 11;
            this.lblStrictLow.Text = "Strict Low:";

            // 
            // txtStrictLow
            // 
            this.txtStrictLow = new System.Windows.Forms.TextBox();
            this.txtStrictLow.Location = new System.Drawing.Point(482, 87);
            this.txtStrictLow.Name = "txtStrictLow";
            this.txtStrictLow.Size = new System.Drawing.Size(66, 27);
            this.txtStrictLow.TabIndex = 12;
            this.txtStrictLow.Enabled = false;

            // add to Controls
            this.Controls.Add(this.lblStrictHigh);
            this.Controls.Add(this.txtStrictHigh);
            this.Controls.Add(this.lblStrictMedium);
            this.Controls.Add(this.txtStrictMedium);
            this.Controls.Add(this.lblStrictLow);
            this.Controls.Add(this.txtStrictLow);

            // 
            // btnOK
            // 
            this.btnOK.Location = new System.Drawing.Point(200, 180);
            this.btnOK.Size = new System.Drawing.Size(94, 29);
            this.btnOK.Name = "btnOK";
            this.btnOK.TabIndex = 5;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);

            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(310, 180);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(94, 29);
            this.btnCancel.TabIndex = 6;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);

            // 
            // SettingsForm
            // 

            this.ClientSize = new System.Drawing.Size(600, 220);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.cmbFrameRate);
            this.Controls.Add(this.lblFrameRate);
            this.Controls.Add(this.btnBrowse);
            this.Controls.Add(this.txtVideoToolPath);
            this.Controls.Add(this.lblVideoToolPath);
            this.Name = "SettingsForm";
            this.Text = "Settings";

            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.Label lblVideoToolPath;
        private System.Windows.Forms.TextBox txtVideoToolPath;
        private System.Windows.Forms.Button btnBrowse;
        private System.Windows.Forms.Label lblFrameRate;
        private System.Windows.Forms.ComboBox cmbFrameRate;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnCancel;

        private System.Windows.Forms.Label lblStrictHigh;
        private System.Windows.Forms.TextBox txtStrictHigh;
        private System.Windows.Forms.Label lblStrictMedium;
        private System.Windows.Forms.TextBox txtStrictMedium;
        private System.Windows.Forms.Label lblStrictLow;
        private System.Windows.Forms.TextBox txtStrictLow;
    }
}
