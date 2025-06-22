﻿using SimpleVideoCutter.Properties;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Forms;

namespace SimpleVideoCutter
{
    partial class AboutBox : Form
    {
        public AboutBox()
        {
            InitializeComponent();
            this.Text = $"{GlobalStrings.AboutBox_About} {AssemblyTitle}";
            this.labelProductName.Text = AssemblyProduct;
            this.labelVersion.Text = $"{GlobalStrings.AboutBox_Version} {AssemblyVersion}";
            this.labelCopyright.Text = AssemblyCopyright;

        }


        #region Assembly Attribute Accessors

        public string AssemblyTitle
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
                if (attributes.Length > 0)
                {
                    AssemblyTitleAttribute titleAttribute = (AssemblyTitleAttribute)attributes[0];
                    if (titleAttribute.Title != "")
                    {
                        return titleAttribute.Title;
                    }
                }
                return System.IO.Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().Location);
            }
        }

        public string AssemblyVersion
        {
            get
            {
                return Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0.0";
            }
        }

        public string AssemblyDescription
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyDescriptionAttribute)attributes[0]).Description;
            }
        }

        public string AssemblyProduct
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyProductAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyProductAttribute)attributes[0]).Product;
            }
        }

        public string AssemblyCopyright
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyCopyrightAttribute)attributes[0]).Copyright;
            }
        }

        public string AssemblyCompany
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCompanyAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyCompanyAttribute)attributes[0]).Company;
            }
        }
        #endregion

        private void linkLabelGithub_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
          //  Process.Start(new ProcessStartInfo(linkLabelGithub.Text) { UseShellExecute = true });
        }
        private void linkLabelGithubreleases_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
          //  Process.Start(new ProcessStartInfo(linkLabelGithubReleases.Text) { UseShellExecute = true });
        }

        private void AboutBox_Load(object sender, EventArgs e)
        {
            if (Updater.Instance.NewVersionDownloaded)
            {
                this.labelVersion.Text = $"{GlobalStrings.AboutBox_Version} {AssemblyVersion}"
                    + $" - {GlobalStrings.AboutBox_NewVersionDownloaded}";
            }
            else if (Updater.Instance.NewVersionAvailable)
            {
                this.labelVersion.Text = $"{GlobalStrings.AboutBox_Version} {AssemblyVersion}"
                    + $" - {GlobalStrings.AboutBox_NewVersionAvailable}";
            }
            else
            {
                this.labelVersion.Text = $"{GlobalStrings.AboutBox_Version} {AssemblyVersion}"
                    + $" - {GlobalStrings.AboutBox_VersionUpToDate}";
            }
        }

        private void linkLabelEmail_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("mailto:xyz@gmails.com") { UseShellExecute = true });
        }

        private void buttonLicense_Click(object sender, EventArgs e)
        {
            new LicenseBox().ShowDialog();
        }
    }
}
