using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerService
{
    public class AppSettingsOptions
    {
        public int SuspectReloadIntervalMinutes { get; set; } = 2;
        public double MatchThreshold { get; set; } = 0.70;

        public string APIURL { get; set; } = null;
        public int SiteId { get; set; } = 0;
    }
}
