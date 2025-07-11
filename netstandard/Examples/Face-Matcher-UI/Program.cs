using Accord.Statistics;
using System.Xml.Linq;

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
            AppState.LoadSettingsFromXml("appsettings.xml");
            Application.Run(new Form1());
        }

    }
    public static class AppState
    {
        public static bool IsTrial { get; set; } = false;
        public static bool isAdmin { get; set; } = false;
        
        public static string VideoToolPath { get; set; } = "";
        public static double FrameRate { get; set; } = 1.0;

        public static double StrictHigh { get; set; } = 0.65;
        public static double StrictMedium { get; set; } = 0.70;
        public static double StrictLow { get; set; } = 0.75;
        public static void LoadSettingsFromXml(string filePath)
        {
            if (!File.Exists(filePath))
            {
                // Create XML with default values
                var defaultXml = new XDocument(
                    new XElement("Settings",
                        new XElement("VideoToolPath", ""),
                        new XElement("FrameRate", "1.0"),
                        new XElement("StrictHigh", "0.65"),
                        new XElement("StrictMedium", "0.70"),
                        new XElement("StrictLow", "0.75")
                    )
                );
                defaultXml.Save(filePath);
            }

            // Now load the file
            var xml = XDocument.Load(filePath);
            var settings = xml.Element("Settings");
            if (settings == null)
                return;

            AppState.VideoToolPath = settings.Element("VideoToolPath")?.Value ?? "";
            AppState.FrameRate = double.TryParse(settings.Element("FrameRate")?.Value, out double frameRate) ? frameRate : 1.0;

            AppState.StrictHigh = double.TryParse(settings.Element("StrictHigh")?.Value, out double high) ? high : 0.65;
            AppState.StrictMedium = double.TryParse(settings.Element("StrictMedium")?.Value, out double med) ? med : 0.70;
            AppState.StrictLow = double.TryParse(settings.Element("StrictLow")?.Value, out double low) ? low : 0.75;
        }


    }
}