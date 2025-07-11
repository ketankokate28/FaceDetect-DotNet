using System;
using System.IO;
using System.Windows.Forms;
using System.Xml.Linq;

namespace Face_Matcher_UI
{
    public partial class SettingsForm : Form
    {
        public SettingsForm()
        {
            InitializeComponent();
            LoadSettings();
        }

        private void LoadSettings()
        {
            txtVideoToolPath.Text = AppState.VideoToolPath ?? "";

            // Convert 1.0 → 1, etc.
            if (AppState.FrameRate == 1.0)
                cmbFrameRate.SelectedItem = "1";
            else if (AppState.FrameRate == 2.0)
                cmbFrameRate.SelectedItem = "2";
            else if (AppState.FrameRate == 3.0)
                cmbFrameRate.SelectedItem = "3";
            else
                cmbFrameRate.SelectedIndex = 0; // fallback

            txtStrictHigh.Text = AppState.StrictHigh.ToString("0.00");
            txtStrictMedium.Text = AppState.StrictMedium.ToString("0.00");
            txtStrictLow.Text = AppState.StrictLow.ToString("0.00");
        }
        private void btnBrowse_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Title = "Select Video Tool EXE";
                ofd.Filter = "Executable Files|*.exe|All Files|*.*";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    txtVideoToolPath.Text = ofd.FileName;
                }
            }
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            AppState.VideoToolPath = txtVideoToolPath.Text.Trim();

            if (!ValidateStrictValue(txtStrictHigh.Text.Trim(), out double high, "Strict High")) return;
            if (!ValidateStrictValue(txtStrictMedium.Text.Trim(), out double medium, "Strict Medium")) return;
            if (!ValidateStrictValue(txtStrictLow.Text.Trim(), out double low, "Strict Low")) return;

            AppState.StrictHigh = high;
            AppState.StrictMedium = medium;
            AppState.StrictLow = low;

            switch (cmbFrameRate.SelectedItem?.ToString())
            {
                case "1":
                    AppState.FrameRate = 1.0;
                    break;
                case "2":
                    AppState.FrameRate = 2.0;
                    break;
                case "3":
                    AppState.FrameRate = 3.0;
                    break;
                default:
                    AppState.FrameRate = 1.0;
                    break;
            }

            SaveToXml();

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
        private bool ValidateStrictValue(string input, out double value, string fieldName)
        {
            value = 0.0;

            if (!double.TryParse(input, out value))
            {
                MessageBox.Show($"{fieldName} must be a valid decimal number.", "Validation Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (value < 0.0 || value > 1.0)
            {
                MessageBox.Show($"{fieldName} must be between 0.0 and 1.0.", "Validation Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            return true;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void SaveToXml()
        {
            string xmlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.xml");
            XDocument doc;

            if (File.Exists(xmlPath))
            {
                doc = XDocument.Load(xmlPath);
            }
            else
            {
                doc = new XDocument(new XElement("Settings"));
            }

            var root = doc.Root;

            root.SetElementValue("VideoToolPath", AppState.VideoToolPath);
            root.SetElementValue("FrameRate", AppState.FrameRate);
            root.SetElementValue("StrictHigh", AppState.StrictHigh);
            root.SetElementValue("StrictMedium", AppState.StrictMedium);
            root.SetElementValue("StrictLow", AppState.StrictLow);

            doc.Save(xmlPath);
        }
        public void EnableStrictFields()
        {
            txtStrictHigh.Enabled = true;
            txtStrictMedium.Enabled = true;
            txtStrictLow.Enabled = true;
        }
    }
}
