using System.Collections.Generic;
using Verse;

namespace WorkMonitor.Tracking
{
    public class WorkMonitorSidecarData : IExposable
    {
        public List<PawnWorkGiverRecords> savedPawnRecords = new List<PawnWorkGiverRecords>();
        public List<PawnWorkGiverHistory> savedPawnHistory = new List<PawnWorkGiverHistory>();
        public List<GroupHistoryEntry> savedGroupHistory = new List<GroupHistoryEntry>();
        public List<ColonistWorkProfileEntry> savedColonistProfiles = new List<ColonistWorkProfileEntry>();

        public MapWorkSnapshot latestSnapshot;
        public List<MapWorkSnapshot> historyBuffer = new List<MapWorkSnapshot>();
        public Dictionary<string, int> taskFirstSeenDayId = new Dictionary<string, int>();
        public int lastSampledHour = -1;

        public void ExposeData()
        {
            Scribe_Collections.Look(ref savedPawnRecords, "savedPawnRecords", LookMode.Deep);
            Scribe_Collections.Look(ref savedPawnHistory, "savedPawnHistory", LookMode.Deep);
            Scribe_Collections.Look(ref savedGroupHistory, "savedGroupHistory", LookMode.Deep);
            Scribe_Collections.Look(ref savedColonistProfiles, "savedColonistProfiles", LookMode.Deep);
            Scribe_Deep.Look(ref latestSnapshot, "latestSnapshot");
            Scribe_Collections.Look(ref historyBuffer, "historyBuffer", LookMode.Deep);
            Scribe_Collections.Look(ref taskFirstSeenDayId, "taskFirstSeenDayId", LookMode.Value, LookMode.Value);
            Scribe_Values.Look(ref lastSampledHour, "lastSampledHour", -1);

            savedPawnRecords ??= new List<PawnWorkGiverRecords>();
            savedPawnHistory ??= new List<PawnWorkGiverHistory>();
            savedGroupHistory ??= new List<GroupHistoryEntry>();
            savedColonistProfiles ??= new List<ColonistWorkProfileEntry>();
            historyBuffer ??= new List<MapWorkSnapshot>();
            taskFirstSeenDayId ??= new Dictionary<string, int>();
        }

        public bool HasAnyData()
        {
            if (savedPawnRecords.Count > 0
                || savedPawnHistory.Count > 0
                || savedGroupHistory.Count > 0
                || savedColonistProfiles.Count > 0)
            {
                return true;
            }

            if (latestSnapshot != null || historyBuffer.Count > 0 || taskFirstSeenDayId.Count > 0)
            {
                return true;
            }

            return false;
        }
    }
}
