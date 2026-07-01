using System.Diagnostics;

namespace WorkMonitor.Diagnostics
{
    public struct WorkMonitorPerfScope : System.IDisposable
    {
        private readonly string category;
        private readonly long start;

        public WorkMonitorPerfScope(string category)
        {
            this.category = category;
            start = WorkMonitorPerfRecorder.Enabled ? Stopwatch.GetTimestamp() : 0;
        }

        public void Dispose()
        {
            if (start != 0)
            {
                WorkMonitorPerfRecorder.Record(category, Stopwatch.GetTimestamp() - start);
            }
        }
    }
}
