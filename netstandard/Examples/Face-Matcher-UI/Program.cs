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
            // Initialize app if license is valid
            ApplicationConfiguration.Initialize();
            DbHelper.Initialize(); // Ensure DB is ready

            bool licenseValid = false;

            while (!licenseValid)
            {
                // If license file doesn't exist, prompt user
                if (!File.Exists("mylicense.lic"))
                {
                    var ofd = new OpenFileDialog
                    {
                        Title = "Select Your License File",
                        Filter = "License Files (*.lic)|*.lic|All Files (*.*)|*.*"
                    };

                    if (ofd.ShowDialog() == DialogResult.OK)
                    {
                        // Copy to a temp file first
                        string tempLicensePath = Path.Combine(Path.GetTempPath(), "temp_license.lic");
                        File.Copy(ofd.FileName, tempLicensePath, true);

                        // Validate temp license file before accepting it
                        var tempChecker = new check();
                        if (tempChecker.checklisence(tempLicensePath)) // overload method
                        {
                            File.Copy(tempLicensePath, "mylicense.lic", true); // now save it permanently
                            licenseValid = true;
                        }
                        else
                        {
                            MessageBox.Show("Invalid or expired license. Please select a valid license.", "License Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            File.Delete(tempLicensePath); // clean up temp file
                        }
                    }
                    else
                    {
                        MessageBox.Show("License file is required to run the application.", "Missing License", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }
                else
                {
                    // License file exists — validate it
                    var checker = new check();
                    if (checker.checklisence("mylicense.lic"))
                    {
                        licenseValid = true;
                    }
                    else
                    {
                        // Invalid file — delete it and retry
                        MessageBox.Show("Saved license file is invalid or expired. Please upload a new license.", "License Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        File.Delete("mylicense.lic");
                    }
                }
            }



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