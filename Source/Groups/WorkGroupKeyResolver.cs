using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace WorkMonitor.Groups
{
    public static class WorkGroupKeyResolver
    {
        public static IEnumerable<string> ResolveGroupKeysForWorkGiver(WorkGiverDef workGiver)
        {
            if (workGiver == null)
            {
                yield break;
            }

            yield return WorkGroupKey.ForWorkType(workGiver.workType).StorageKey;

            if (ModsConfig.IsActive("philip2p2026.worktabgroups"))
            {
                string customKey = WorkTabGroupsProvider.GetGroupKeyForWorkGiver(workGiver);
                if (!customKey.NullOrEmpty())
                {
                    yield return customKey;
                }
            }
        }

        public static HashSet<WorkGiverDef> GetAssignedWorkGivers()
        {
            HashSet<WorkGiverDef> assigned = new HashSet<WorkGiverDef>();
            foreach (WorkTypeDef workType in DefDatabase<WorkTypeDef>.AllDefsListForReading)
            {
                if (workType.workGiversByPriority != null)
                {
                    foreach (WorkGiverDef wg in workType.workGiversByPriority)
                    {
                        assigned.Add(wg);
                    }
                }
            }

            if (ModsConfig.IsActive("philip2p2026.worktabgroups"))
            {
                foreach (WorkGiverDef wg in WorkTabGroupsProvider.GetAssignedWorkGivers())
                {
                    assigned.Add(wg);
                }
            }

            return assigned;
        }
    }
}
