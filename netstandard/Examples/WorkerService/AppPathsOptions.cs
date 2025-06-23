using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerService
{
    public class AppPathsOptions
    {
        public string ResultDir { get; set; } = string.Empty;
        public string TempResultDir { get; set; } = string.Empty;
        public string FramesDir { get; set; } = string.Empty;
        public string SuspectDir { get; set; } = string.Empty;
    }
}
