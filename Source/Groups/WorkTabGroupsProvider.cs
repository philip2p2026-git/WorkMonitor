using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using WorkTabGroups;

namespace WorkMonitor.Groups
{
    public static class WorkTabGroupsProvider
    {
        private const string PackageId = "philip2p2026.worktabgroups";

        public static bool IsIntegrationActive => ModsConfig.IsActive(PackageId);

        public static IEnumerable<WorkGroupSnapshot> GetCustomGroups()
        {
            if (!IsIntegrationActive)
            {
                yield break;
            }

            WorkTabGroupsManager manager = WorkTabGroupsManager.Instance;
            if (manager == null)
            {
                yield break;
            }

            foreach (MajorWorkGroupData group in manager.Groups)
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
            if (!IsIntegrationActive || workGiver == null)
            {
                return null;
            }

            WorkTabGroupsManager manager = WorkTabGroupsManager.Instance;
            MajorWorkGroupData group = manager?.GetGroupForWorkGiver(workGiver);
            if (group == null || group.defName.NullOrEmpty())
            {
                return null;
            }

            return WorkGroupKey.ForCustomGroup(group.defName).StorageKey;
        }

        public static IEnumerable<WorkGiverDef> GetAssignedWorkGivers()
        {
            if (!IsIntegrationActive)
            {
                yield break;
            }

            WorkTabGroupsManager manager = WorkTabGroupsManager.Instance;
            if (manager == null)
            {
                yield break;
            }

            foreach (KeyValuePair<WorkGiverDef, MajorWorkGroupData> assignment in manager.GetAssignedWorkGiverAssignments())
            {
                if (assignment.Key != null)
                {
                    yield return assignment.Key;
                }
            }
        }

        public static IReadOnlyList<WorkLayoutEntry> GetWorkLayoutOrder()
        {
            if (!IsIntegrationActive)
            {
                return null;
            }

            WorkTabGroupsManager manager = WorkTabGroupsManager.Instance;
            return manager?.WorkLayoutOrder;
        }

        private static WorkGroupSnapshot TryMapGroup(MajorWorkGroupData group)
        {
            if (group == null || group.defName.NullOrEmpty())
            {
                return null;
            }

            List<WorkGiverDef> workGivers = new List<WorkGiverDef>();
            if (group.assignedWorkGiverDefNames != null)
            {
                foreach (string wgName in group.assignedWorkGiverDefNames)
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
                Key = WorkGroupKey.ForCustomGroup(group.defName),
                Label = group.label.NullOrEmpty() ? group.defName : group.label,
                WorkGivers = workGivers,
                UniqueWorkTypes = workTypes,
                PrimaryWorkType = workTypes.FirstOrDefault()
            };
        }
    }
}
