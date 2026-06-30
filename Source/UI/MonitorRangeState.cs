using WorkMonitor;

namespace WorkMonitor.UI
{
    public class MonitorRangeState
    {
        public int RangeHours { get; private set; }

        public MonitorRangeState()
        {
            RangeHours = MonitorRangeState.NormalizeRangeHours(WorkMonitorMod.Settings?.statsWindowHours ?? 24);
        }

        public int MinHourIndex => WorkMonitorUtility.CurrentHourIndex() - RangeHours;

        public void CycleRangeHours()
        {
            RangeHours = RangeHours switch
            {
                6 => 12,
                12 => 24,
                24 => 48,
                _ => 6
            };
        }

        public static int NormalizeRangeHours(int hours)
        {
            return hours switch
            {
                <= 6 => 6,
                <= 12 => 12,
                <= 24 => 24,
                _ => 48
            };
        }
    }
}
