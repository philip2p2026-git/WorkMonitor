using RimWorld;
using Verse;

namespace WorkMonitor.Groups
{
    public class ColonistWorkStat
    {
        public Pawn Pawn;
        public string Label;
        public int JobCount;
        public int EndlessJobCount;
        public int TicksSpent;
        public int TravelTicksSpent;
        public int WorkTicksSpent;
        public float WorkUnitsSpent;
        public float JobsPerHour;
        public float WorkUnitsPerHour;
        public Passion Passion;
    }

    public class WorkGiverStat
    {
        public WorkGiverDef WorkGiver;
        public string Label;
        public int JobCount;
        public int EndlessJobCount;
        public int TicksSpent;
        public int TravelTicksSpent;
        public int WorkTicksSpent;
        public float WorkUnitsSpent;
    }

    public class ColonistGroupStat
    {
        public WorkGroupSnapshot Group;
        public int MapOpenTasks;
        public float MapWorkLeft;
        public int JobCount;
        public int EndlessJobCount;
        public int TicksSpent;
        public int TravelTicksSpent;
        public int WorkTicksSpent;
        public float WorkUnitsSpent;
        public int GroupJobCount;
        public float GroupWorkUnits;
        public int GroupTicksSpent;
    }

    public class ColonistWorkGiverStat
    {
        public WorkGiverDef WorkGiver;
        public string Label;
        public int JobCount;
        public int EndlessJobCount;
        public int TravelTicksSpent;
        public int WorkTicksSpent;
        public float WorkUnitsSpent;
    }

    public class ColonistGroupWorkDetail
    {
        public WorkGroupSnapshot Group;
        public Pawn Pawn;
        public int JobCount;
        public int EndlessJobCount;
        public int TravelTicksSpent;
        public int WorkTicksSpent;
        public float WorkUnitsSpent;
        public System.Collections.Generic.List<ColonistWorkGiverStat> WorkGiverStats = new System.Collections.Generic.List<ColonistWorkGiverStat>();
    }

    public class ColonistStats
    {
        public Pawn Pawn;
        public string Label;
        public Passion TopPassion;
        public int TotalJobCount;
        public int TotalEndlessJobCount;
        public int TotalTicksSpent;
        public int TotalTravelTicksSpent;
        public int TotalWorkTicksSpent;
        public float TotalWorkUnits;
        public System.Collections.Generic.List<ColonistGroupStat> GroupStats = new System.Collections.Generic.List<ColonistGroupStat>();
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
        public int TotalEndlessJobCount;
        public int TotalTicksSpent;
        public float TotalWorkUnits;
        public int TotalMapOpenTasks;
        public float TotalMapWorkLeft;
        public int MapSampleTick;
        public System.Collections.Generic.List<WorkGiverStat> WorkGiverStats = new System.Collections.Generic.List<WorkGiverStat>();
        public System.Collections.Generic.List<ColonistWorkStat> ColonistStats = new System.Collections.Generic.List<ColonistWorkStat>();
    }
}
