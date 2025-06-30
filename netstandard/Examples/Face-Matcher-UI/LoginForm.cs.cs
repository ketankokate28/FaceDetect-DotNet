using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace Face_Matcher_UI
{
    public partial class LoginForm : Form
    {
        public bool IsAuthenticated { get; private set; } = false;
        public LoginForm()
        {
            InitializeComponent();

            btnLogin.Click += (s, e) =>
            {
                var user = txtUsername.Text.Trim();
                var pass = txtPassword.Text.Trim();

                // 🔐 Hardcoded credentials (you can externalize if needed)
                if (user == "admin" && pass == "pass123")
                {
                    IsAuthenticated = true;
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Invalid credentials!", "Login Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    IsAuthenticated = false;
                }
            };

            this.AcceptButton = btnLogin;
        }
    }
}
