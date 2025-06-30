using Accord.Statistics;

namespace Face_Matcher_UI
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            DbHelper.Initialize(); // <- Ensure DB and table are ready
                                   // To customize application configuration such as set high DPI settings or default font,
                                   // see https://aka.ms/applicationconfiguration.


            if (!File.Exists("mylicense.lic"))
            {
                OpenFileDialog ofd = new OpenFileDialog
                {
                    Title = "Select Your License File",
                    Filter = "License Files (*.lic)|*.lic|All Files (*.*)|*.*"
                };

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    File.Copy(ofd.FileName, "mylicense.lic");
                }
                else
                {
                    MessageBox.Show("License file is required to run the app.");
                    return;
                }
            }

            check obj = new check();
          bool result =  obj.checklisence();
            if (!result)
            {
                MessageBox.Show("License validation failed or expired.", "License Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(0);
                return;
            }
            ApplicationConfiguration.Initialize();

            // Show login screen
            var loginForm = new LoginForm();
            loginForm.ShowDialog();

            if (!loginForm.IsAuthenticated)
            {
                MessageBox.Show("Login required to proceed.", "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                Environment.Exit(0);
                return;
            }

            Application.Run(new Form1());
        }
    }
}