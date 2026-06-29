using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace WorkMonitor.Groups
{
    public class WorkTypeGroupProvider : IWorkGroupProvider
    {
        public IEnumerable<WorkGroupSnapshot> GetGroups()
        {
            foreach (WorkTypeDef workType in DefDatabase<WorkTypeDef>.AllDefsListForReading.OrderBy(w => w.label))
            {
                if (workType.workGiversByPriority == null || workType.workGiversByPriority.Count == 0)
                {
                    continue;
                }

                yield return new WorkGroupSnapshot
                {
                    Key = WorkGroupKey.ForWorkType(workType),
                    Label = workType.label,
                    WorkGivers = workType.workGiversByPriority.ToList(),
                    UniqueWorkTypes = new List<WorkTypeDef> { workType },
                    PrimaryWorkType = workType
                };
            }
        }
    }
}
