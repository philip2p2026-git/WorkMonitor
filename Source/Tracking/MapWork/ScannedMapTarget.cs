using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using WorkMonitor.Groups;

namespace WorkMonitor.Tracking.MapWork
{
    public readonly struct ScannedMapTarget
    {
        public readonly string DedupeKey;
        public readonly float WorkLeft;
        public readonly List<string> WorkGiverDefNames;
        public readonly List<string> GroupKeys;

        public ScannedMapTarget(string dedupeKey, float workLeft, List<WorkGiverDef> workGivers, List<string> groupKeys = null)
        {
            DedupeKey = dedupeKey;
            WorkLeft = workLeft;
            WorkGiverDefNames = workGivers?.Where(wg => wg != null).Select(wg => wg.defName).Distinct().ToList()
                ?? new List<string>();
            GroupKeys = groupKeys ?? MapWorkAttribution.GroupKeysFor(workGivers);
        }
    }
}
