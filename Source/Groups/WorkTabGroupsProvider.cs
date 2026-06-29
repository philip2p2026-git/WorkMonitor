using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using Verse;

namespace WorkMonitor.Groups
{
    public static class WorkTabGroupsProvider
    {
        private const string PackageId = "philip2p2026.worktabgroups";
        private static bool reflectionFailed;

        public static IEnumerable<WorkGroupSnapshot> GetCustomGroups()
        {
            if (!ModsConfig.IsActive(PackageId) || reflectionFailed)
            {
                yield break;
            }

            object manager = GetManagerInstance();
            if (manager == null)
            {
                yield break;
            }

            PropertyInfo groupsProp = manager.GetType().GetProperty("Groups", BindingFlags.Public | BindingFlags.Instance);
            if (groupsProp?.GetValue(manager) is not System.Collections.IEnumerable groups)
            {
                yield break;
            }

            foreach (object group in groups)
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
            object manager = GetManagerInstance();
            if (manager == null || workGiver == null)
            {
                return null;
            }

            MethodInfo method = manager.GetType().GetMethod("GetGroupForWorkGiver", BindingFlags.Public | BindingFlags.Instance);
            if (method == null)
            {
                return null;
            }

            object group = method.Invoke(manager, new object[] { workGiver });
            if (group == null)
            {
                return null;
            }

            string defName = group.GetType().GetField("defName", BindingFlags.Public | BindingFlags.Instance)?.GetValue(group) as string;
            if (defName.NullOrEmpty())
            {
                return null;
            }

            return WorkGroupKey.ForCustomGroup(defName).StorageKey;
        }

        public static IEnumerable<WorkGiverDef> GetAssignedWorkGivers()
        {
            object manager = GetManagerInstance();
            if (manager == null)
            {
                yield break;
            }

            MethodInfo method = manager.GetType().GetMethod("GetAssignedWorkGiverAssignments", BindingFlags.Public | BindingFlags.Instance);
            if (method?.Invoke(manager, null) is not System.Collections.IEnumerable assignments)
            {
                yield break;
            }

            foreach (object assignment in assignments)
            {
                if (assignment == null)
                {
                    continue;
                }

                Type kvpType = assignment.GetType();
                PropertyInfo keyProp = kvpType.GetProperty("Key");
                if (keyProp?.GetValue(assignment) is WorkGiverDef wg)
                {
                    yield return wg;
                }
            }
        }

        private static object GetManagerInstance()
        {
            try
            {
                Type managerType = GenTypes.GetTypeInAnyAssembly("WorkTabGroups.WorkTabGroupsManager");
                if (managerType == null)
                {
                    reflectionFailed = true;
                    return null;
                }

                PropertyInfo instanceProp = managerType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                return instanceProp?.GetValue(null);
            }
            catch (Exception ex)
            {
                reflectionFailed = true;
                Log.Warning("[WorkMonitor] WorkTabGroups reflection failed: " + ex.Message);
                return null;
            }
        }

        private static WorkGroupSnapshot TryMapGroup(object group)
        {
            if (group == null)
            {
                return null;
            }

            Type type = group.GetType();
            string defName = type.GetField("defName", BindingFlags.Public | BindingFlags.Instance)?.GetValue(group) as string;
            string label = type.GetField("label", BindingFlags.Public | BindingFlags.Instance)?.GetValue(group) as string;
            if (defName.NullOrEmpty())
            {
                return null;
            }

            List<string> wgNames = type.GetField("assignedWorkGiverDefNames", BindingFlags.Public | BindingFlags.Instance)?.GetValue(group) as List<string>;
            List<WorkGiverDef> workGivers = new List<WorkGiverDef>();
            if (wgNames != null)
            {
                foreach (string wgName in wgNames)
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

            List<WorkTypeDef> workTypes = workGivers.Select(w => w.workType).Distinct().ToList();
            return new WorkGroupSnapshot
            {
                Key = WorkGroupKey.ForCustomGroup(defName),
                Label = label ?? defName,
                WorkGivers = workGivers,
                UniqueWorkTypes = workTypes,
                PrimaryWorkType = workTypes.FirstOrDefault()
            };
        }
    }
}
