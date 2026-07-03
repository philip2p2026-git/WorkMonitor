using System.IO;
using Verse;

namespace WorkMonitor.Tracking
{
    public static class WorkMonitorSaveTracker
    {
        public static string CurrentSaveName { get; private set; }

        public static void SetFromPath(string filepath)
        {
            if (filepath.NullOrEmpty())
            {
                return;
            }

            CurrentSaveName = Path.GetFileNameWithoutExtension(filepath);
        }
    }
}
