namespace Face_Matcher_UI
{
    partial class LoginForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        //private System.ComponentModel.IContainer components = null;

        private PictureBox pictureLogo;
        private Label labelTitle;
        private Label labelUsername;
        private TextBox txtUsername;
        private Label labelPassword;
        private TextBox txtPassword;
        private Button btnLogin;
        private void InitializeComponent()
        {
            this.pictureLogo = new PictureBox();
            this.labelTitle = new Label();
            this.labelUsername = new Label();
            this.txtUsername = new TextBox();
            this.labelPassword = new Label();
            this.txtPassword = new TextBox();
            this.btnLogin = new Button();

            ((System.ComponentModel.ISupportInitialize)(this.pictureLogo)).BeginInit();
            this.SuspendLayout();

            // === Logo ===
            this.pictureLogo.Image = Image.FromFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logo.png"));
            this.pictureLogo.SizeMode = PictureBoxSizeMode.Zoom;
            this.pictureLogo.Location = new Point(30, 20);
            this.pictureLogo.Size = new Size(320, 100);

            // === Title ===
            this.labelTitle.Text = "Secure Login";
            this.labelTitle.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            this.labelTitle.TextAlign = ContentAlignment.MiddleCenter;
            this.labelTitle.Location = new Point(30, 125);
            this.labelTitle.Size = new Size(320, 40);

            // === Username ===
            this.labelUsername.Text = "Username";
            this.labelUsername.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
            this.labelUsername.Location = new Point(30, 180);
            this.labelUsername.Size = new Size(100, 25);

            this.txtUsername.Font = new Font("Segoe UI", 10F);
            this.txtUsername.Location = new Point(30, 205);
            this.txtUsername.Size = new Size(320, 25);

            // === Password ===
            this.labelPassword.Text = "Password";
            this.labelPassword.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
            this.labelPassword.Location = new Point(30, 245);
            this.labelPassword.Size = new Size(100, 25);

            this.txtPassword.Font = new Font("Segoe UI", 10F);
            this.txtPassword.Location = new Point(30, 270);
            this.txtPassword.Size = new Size(320, 25);
            this.txtPassword.PasswordChar = '●';

            // === Login Button ===
            this.btnLogin.Text = "Login";
            this.btnLogin.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            this.btnLogin.BackColor = Color.MediumSlateBlue;
            this.btnLogin.ForeColor = Color.White;
            this.btnLogin.FlatStyle = FlatStyle.Flat;
            this.btnLogin.FlatAppearance.BorderSize = 0;
            this.btnLogin.Location = new Point(30, 315);
            this.btnLogin.Size = new Size(320, 35);
            this.btnLogin.TabIndex = 3;

            // === Form ===
            this.AcceptButton = this.btnLogin;
            this.AutoScaleDimensions = new SizeF(8F, 20F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(390, 390);
            this.Controls.Add(this.pictureLogo);
            this.Controls.Add(this.labelTitle);
            this.Controls.Add(this.labelUsername);
            this.Controls.Add(this.txtUsername);
            this.Controls.Add(this.labelPassword);
            this.Controls.Add(this.txtPassword);
            this.Controls.Add(this.btnLogin);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Text = "Login";

            ((System.ComponentModel.ISupportInitialize)(this.pictureLogo)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion
    }
}