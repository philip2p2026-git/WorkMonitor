using RimWorld;
using Verse;

namespace WorkMonitor.Groups
{
    public class ColonistWorkStat
    {
        public Pawn Pawn;
        public string Label;
        public int JobCount;
        public int TicksSpent;
        public float PercentOfGroupTime;
        public Passion Passion;
    }

    public class WorkGiverStat
    {
        public WorkGiverDef WorkGiver;
        public string Label;
        public int JobCount;
        public int TicksSpent;
    }

    public class WorkGroupStats
    {
        public WorkGroupSnapshot Group;
        public WorkActivityStatus Status;
        public int CapableCount;
        public int EnabledCount;
        public int WorkedCount;
        public int MajorInterestCount;
        public int MinorInterestCount;
        public int TotalJobCount;
        public int TotalTicksSpent;
        public System.Collections.Generic.List<WorkGiverStat> WorkGiverStats = new System.Collections.Generic.List<WorkGiverStat>();
        public System.Collections.Generic.List<ColonistWorkStat> ColonistStats = new System.Collections.Generic.List<ColonistWorkStat>();
    }
}
