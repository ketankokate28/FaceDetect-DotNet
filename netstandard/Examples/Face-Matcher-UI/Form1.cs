using System.Windows.Forms;

namespace Face_Matcher_UI
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            txtLog.Dock = DockStyle.Fill;
            txtLog.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string suspectDir = txtSuspectDir.Text;
            string imageDir = txtImageDir.Text;
            string resultDir = txtResultDir.Text;
            if (!Directory.Exists(suspectDir) || !Directory.Exists(imageDir))
            {
                MessageBox.Show("Please select valid directories.");
                return;
            }
            Directory.CreateDirectory(resultDir);
            button1.Enabled = false;
            button1.UseVisualStyleBackColor = false; // Allow custom BackColor
            button1.BackColor = Color.Red; // Red when disabled

            var matcher = new FaceMatcher();

            Task.Run(() =>
            {
                matcher.Run(suspectDir, imageDir, resultDir, message =>
                {
                    txtLog.Invoke((MethodInvoker)(() => txtLog.AppendText(message + Environment.NewLine)));
                });

                txtLog.Invoke((MethodInvoker)(() =>
                {
                    button1.Enabled = true;
                    button1.BackColor = SystemColors.Control; // Reset to default system color
                }));
            });


        }
    }
}
