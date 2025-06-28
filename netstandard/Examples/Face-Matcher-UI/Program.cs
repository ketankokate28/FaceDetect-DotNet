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
            check obj = new check();
          bool result =  obj.checklisence();
            if (!result)
            {
                MessageBox.Show("License validation failed or expired.", "License Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(0);
                return;
            }
            ApplicationConfiguration.Initialize();
            Application.Run(new Form1());
        }
    }
}