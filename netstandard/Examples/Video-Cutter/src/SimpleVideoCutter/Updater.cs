using Octokit;
using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Velopack;
using Velopack.Sources;

namespace SimpleVideoCutter
{
    internal class Updater : INotifyPropertyChanged
    {
        public static Updater Instance { get; } = new Updater();
        public event PropertyChangedEventHandler? PropertyChanged;

        public bool NewVersionDownloaded { get; private set; }
        public bool NewVersionAvailable { get; private set; }


        public void StartCheckingVersion()
        {
            Task.Run(async () =>
            {
                await Task.Delay(30000);
                var latestRelease = await GetLatestReleaseVersionFromGitHub();
                NewVersionAvailable = latestRelease != null && latestRelease != Utils.GetCurrentRelease();
                try
                {
                    //Ketan
                  //  var newVersion = await DownloadNewVersion();
                   // NewVersionDownloaded = newVersion != null;
                }
                catch
                {
                    // ignore 
                }            
                NotifyPropertyChanged("NewVersionDownloaded");
                NotifyPropertyChanged("NewVersionAvailable");
            });
        }


        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }


        private async Task<UpdateInfo?> DownloadNewVersion()
        {
            var mgr = new UpdateManager(new GithubSource("https://github.com/bartekmotyl/simple-video-cutter", null, false));
            if (!mgr.IsInstalled)
                return null;

            // check for new version
            var newVersion = await mgr.CheckForUpdatesAsync();
            if (newVersion == null)
                return null; // no update available
            
            // download new version
            await mgr.DownloadUpdatesAsync(newVersion);
            return newVersion;
        }


        private async Task<string?> GetLatestReleaseVersionFromGitHub()
        {
            try
            {
                var client = new GitHubClient(new ProductHeaderValue("simple-video-cutter"));
                client.SetRequestTimeout(TimeSpan.FromSeconds(10));
                var releases = await client.Repository.Release.GetAll("bartekmotyl", "simple-video-cutter");
                var release = releases.FirstOrDefault();
                if (release != null)
                {
                    return release.Name;
                }
            }
            catch (Exception)
            {
                // well, just ingore, can happen when there is no internet connection
            }
            return null;
        }
    }
}
