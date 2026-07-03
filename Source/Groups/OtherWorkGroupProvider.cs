using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace WorkMonitor.Groups
{
    /// <summary>
    /// Orphan work givers from the current DefDatabase only. Save/CSV pawn×workGiver history
    /// for removed defs is retained but not shown here until the mod is active again.
    /// </summary>
    public class OtherWorkGroupProvider : IWorkGroupProvider
    {
        public IEnumerable<WorkGroupSnapshot> GetGroups()
        {
            HashSet<WorkGiverDef> assigned = WorkGroupKeyResolver.GetAssignedWorkGivers();
            List<WorkGiverDef> orphans = DefDatabase<WorkGiverDef>.AllDefsListForReading
                .Where(wg => wg != null && !assigned.Contains(wg))
                .ToList();

            if (orphans.Count == 0)
            {
                yield break;
            }

            yield return new WorkGroupSnapshot
            {
                Key = WorkGroupKey.ForOther(),
                Label = "WorkMonitor.OtherGroup".Translate(),
                WorkGivers = orphans,
                UniqueWorkTypes = orphans.Select(w => w.workType).Where(w => w != null).Distinct().ToList(),
                PrimaryWorkType = orphans.FirstOrDefault()?.workType
            };
        }
    }
}
