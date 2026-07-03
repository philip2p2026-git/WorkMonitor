using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using WorkMonitor.UI;

namespace WorkMonitor.Groups
{
    /// <summary>
    /// Monitor rows from live WorkTypeDef entries. Visibility is def-gated (current DefDatabase only);
    /// pawn×workGiver history in the save is not pruned when defs or mods are removed.
    /// </summary>
    public class WorkTypeGroupProvider : IWorkGroupProvider
    {
        public IEnumerable<WorkGroupSnapshot> GetGroups()
        {
            foreach (WorkTypeDef workType in DefDatabase<WorkTypeDef>.AllDefsListForReading.OrderByDescending(w => w.naturalPriority))
            {
                if (workType.workGiversByPriority == null || workType.workGiversByPriority.Count == 0)
                {
                    continue;
                }

                List<WorkGiverDef> workGivers = CollectWorkGivers(workType);
                if (workGivers.Count == 0)
                {
                    continue;
                }

                yield return new WorkGroupSnapshot
                {
                    Key = WorkGroupKey.ForWorkType(workType),
                    Label = WorkTypeLabelUtility.Format(workType),
                    WorkGivers = workGivers,
                    UniqueWorkTypes = new List<WorkTypeDef> { workType },
                    PrimaryWorkType = workType
                };
            }
        }

        private static List<WorkGiverDef> CollectWorkGivers(WorkTypeDef workType)
        {
            var workGivers = new List<WorkGiverDef>();
            foreach (WorkGiverDef wg in workType.workGiversByPriority)
            {
                if (wg == null)
                {
                    continue;
                }

                if (WorkTabGroupsIntegration.IsAssignedToCustomGroup(wg))
                {
                    continue;
                }

                workGivers.Add(wg);
            }

            return workGivers;
        }
    }
}
