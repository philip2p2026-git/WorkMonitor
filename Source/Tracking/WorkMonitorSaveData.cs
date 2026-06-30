using System.Collections.Generic;
using Verse;

namespace WorkMonitor.Tracking
{
    public class PawnWorkGiverRecords : IExposable
    {
        public int pawnId;
        public Dictionary<string, WorkActivityRecord> records = new Dictionary<string, WorkActivityRecord>();

        public void ExposeData()
        {
            Scribe_Values.Look(ref pawnId, "pawnId");
            Scribe_Collections.Look(ref records, "records", LookMode.Value, LookMode.Deep);
            records ??= new Dictionary<string, WorkActivityRecord>();
        }
    }

    public class PawnWorkGiverHistory : IExposable
    {
        public int pawnId;
        public Dictionary<string, WorkHistoryTierBuffer> history = new Dictionary<string, WorkHistoryTierBuffer>();

        public void ExposeData()
        {
            Scribe_Values.Look(ref pawnId, "pawnId");
            Scribe_Collections.Look(ref history, "history", LookMode.Value, LookMode.Deep);
            history ??= new Dictionary<string, WorkHistoryTierBuffer>();
        }
    }

    public class GroupHistoryEntry : IExposable
    {
        public string groupKey;
        public WorkHistoryTierBuffer buffer = new WorkHistoryTierBuffer();

        public void ExposeData()
        {
            Scribe_Values.Look(ref groupKey, "groupKey");
            Scribe_Deep.Look(ref buffer, "buffer");
            buffer ??= new WorkHistoryTierBuffer();
        }
    }
}
