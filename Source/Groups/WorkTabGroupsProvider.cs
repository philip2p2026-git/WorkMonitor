using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace WorkMonitor.Groups
{
    public static class WorkTabGroupsProvider
    {
        public static bool IsIntegrationActive => WorkTabGroupsIntegration.IsActive;

        public static IEnumerable<WorkGroupSnapshot> GetCustomGroups()
        {
            foreach (WorkTabCustomGroupSnapshot group in WorkTabGroupsIntegration.EnumerateCustomGroups())
            {
                WorkGroupSnapshot snapshot = TryMapGroup(group);
                if (snapshot != null)
                {
                    yield return snapshot;
                }
            }
        }

        public static string GetGroupKeyForWorkGiver(WorkGiverDef workGiver)
        {
            return WorkTabGroupsIntegration.GetCustomGroupStorageKeyForWorkGiver(workGiver);
        }

        public static IEnumerable<WorkGiverDef> GetAssignedWorkGivers()
        {
            return WorkTabGroupsIntegration.EnumerateCustomAssignedWorkGivers();
        }

        public static IReadOnlyList<WorkTabLayoutEntrySnapshot> GetWorkLayoutOrder()
        {
            return WorkTabGroupsIntegration.GetLayoutOrder();
        }

        private static WorkGroupSnapshot TryMapGroup(WorkTabCustomGroupSnapshot group)
        {
            if (group.DefName.NullOrEmpty())
            {
                return null;
            }

            List<WorkGiverDef> workGivers = new List<WorkGiverDef>();
            if (group.AssignedWorkGiverDefNames != null)
            {
                foreach (string wgName in group.AssignedWorkGiverDefNames)
                {
                    WorkGiverDef wg = DefDatabase<WorkGiverDef>.GetNamedSilentFail(wgName);
                    if (wg != null)
                    {
                        workGivers.Add(wg);
                    }
                }
            }

            if (workGivers.Count == 0)
            {
                return null;
            }

            List<WorkTypeDef> workTypes = workGivers
                .Select(w => w.workType)
                .Where(w => w != null)
                .Distinct()
                .ToList();

            return new WorkGroupSnapshot
            {
                Key = WorkGroupKey.ForCustomGroup(group.DefName),
                Label = group.Label.NullOrEmpty() ? group.DefName : group.Label,
                WorkGivers = workGivers,
                UniqueWorkTypes = workTypes,
                PrimaryWorkType = workTypes.FirstOrDefault()
            };
        }
    }
}
