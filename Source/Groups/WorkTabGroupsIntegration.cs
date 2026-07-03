using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using RimWorld;
using Verse;

namespace WorkMonitor.Groups
{
    public readonly struct WorkTabCustomGroupSnapshot
    {
        public readonly string DefName;
        public readonly string Label;
        public readonly IReadOnlyList<string> AssignedWorkGiverDefNames;

        public WorkTabCustomGroupSnapshot(string defName, string label, IReadOnlyList<string> assignedWorkGiverDefNames)
        {
            DefName = defName;
            Label = label;
            AssignedWorkGiverDefNames = assignedWorkGiverDefNames;
        }
    }

    public readonly struct WorkTabLayoutEntrySnapshot
    {
        public readonly string Key;
        public readonly bool IsCustomGroup;

        public WorkTabLayoutEntrySnapshot(string key, bool isCustomGroup)
        {
            Key = key;
            IsCustomGroup = isCustomGroup;
        }
    }

    /// <summary>
    /// Optional integration with Customize your WorkGroup. Uses reflection so WorkMonitor
    /// loads when that mod is absent (no compile-time dependency on WorkTabGroups types).
    /// </summary>
    public static class WorkTabGroupsIntegration
    {
        private const string PackageId = "philip2p2026.worktabgroups";
        private const string ManagerTypeName = "WorkTabGroups.WorkTabGroupsManager";
        private const string LayoutEntryKindTypeName = "WorkTabGroups.WorkLayoutEntryKind";

        private static bool reflectionResolved;
        private static bool reflectionAvailable;
        private static Type managerType;
        private static PropertyInfo instanceProperty;
        private static PropertyInfo groupsProperty;
        private static PropertyInfo layoutOrderProperty;
        private static MethodInfo getGroupForWorkGiverMethod;
        private static MethodInfo isAssignedToCustomGroupMethod;
        private static MethodInfo getAssignedWorkGiverAssignmentsMethod;
        private static FieldInfo groupDefNameField;
        private static FieldInfo groupLabelField;
        private static FieldInfo groupAssignedNamesField;
        private static FieldInfo layoutEntryKeyField;
        private static FieldInfo layoutEntryKindField;
        private static object customGroupKindValue;

        public static bool IsActive => ModsConfig.IsActive(PackageId);

        public static IEnumerable<WorkTabCustomGroupSnapshot> EnumerateCustomGroups()
        {
            object manager = GetManager();
            if (manager == null)
            {
                yield break;
            }

            if (!(groupsProperty.GetValue(manager) is IEnumerable groups))
            {
                yield break;
            }

            foreach (object group in groups)
            {
                WorkTabCustomGroupSnapshot snapshot = MapCustomGroup(group);
                if (!snapshot.DefName.NullOrEmpty())
                {
                    yield return snapshot;
                }
            }
        }

        public static bool IsAssignedToCustomGroup(WorkGiverDef workGiver)
        {
            if (workGiver == null || !TryEnsureReflection())
            {
                return false;
            }

            object manager = GetManager();
            if (manager == null)
            {
                return false;
            }

            object result = isAssignedToCustomGroupMethod.Invoke(manager, new object[] { workGiver });
            return result is bool assigned && assigned;
        }

        public static string GetCustomGroupStorageKeyForWorkGiver(WorkGiverDef workGiver)
        {
            if (workGiver == null || !TryEnsureReflection())
            {
                return null;
            }

            object manager = GetManager();
            if (manager == null)
            {
                return null;
            }

            object group = getGroupForWorkGiverMethod.Invoke(manager, new object[] { workGiver });
            string defName = ReadGroupDefName(group);
            return defName.NullOrEmpty() ? null : WorkGroupKey.ForCustomGroup(defName).StorageKey;
        }

        public static IEnumerable<WorkGiverDef> EnumerateCustomAssignedWorkGivers()
        {
            object manager = GetManager();
            if (manager == null)
            {
                yield break;
            }

            object assignments = getAssignedWorkGiverAssignmentsMethod.Invoke(manager, null);
            if (!(assignments is IEnumerable pairs))
            {
                yield break;
            }

            foreach (object pair in pairs)
            {
                WorkGiverDef wg = ReadKeyValuePairKey(pair) as WorkGiverDef;
                if (wg != null)
                {
                    yield return wg;
                }
            }
        }

        public static IReadOnlyList<WorkTabLayoutEntrySnapshot> GetLayoutOrder()
        {
            object manager = GetManager();
            if (manager == null)
            {
                return null;
            }

            if (!(layoutOrderProperty.GetValue(manager) is IEnumerable entries))
            {
                return null;
            }

            List<WorkTabLayoutEntrySnapshot> result = new List<WorkTabLayoutEntrySnapshot>();
            foreach (object entry in entries)
            {
                if (entry == null)
                {
                    continue;
                }

                string key = layoutEntryKeyField.GetValue(entry) as string;
                if (key.NullOrEmpty())
                {
                    continue;
                }

                object kind = layoutEntryKindField.GetValue(entry);
                bool isCustomGroup = kind != null && kind.Equals(customGroupKindValue);
                result.Add(new WorkTabLayoutEntrySnapshot(key, isCustomGroup));
            }

            return result;
        }

        public static bool TryReadMajorWorkGroupDefName(PawnColumnDef column, out string defName)
        {
            defName = null;
            if (column?.Worker == null)
            {
                return false;
            }

            if (column.workerClass != null
                && !column.workerClass.FullName.Contains("PawnColumnWorker_MajorWorkGroup"))
            {
                return false;
            }

            object boundGroup = column.Worker.GetType().GetProperty("BoundGroup")?.GetValue(column.Worker);
            defName = ReadGroupDefName(boundGroup);
            return !defName.NullOrEmpty();
        }

        private static object GetManager()
        {
            if (!TryEnsureReflection())
            {
                return null;
            }

            return instanceProperty.GetValue(null);
        }

        private static bool TryEnsureReflection()
        {
            if (!IsActive)
            {
                return false;
            }

            if (reflectionResolved)
            {
                return reflectionAvailable;
            }

            reflectionResolved = true;
            reflectionAvailable = false;

            try
            {
                managerType = GenTypes.GetTypeInAnyAssembly(ManagerTypeName);
                if (managerType == null)
                {
                    return false;
                }

                instanceProperty = managerType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                groupsProperty = managerType.GetProperty("Groups", BindingFlags.Public | BindingFlags.Instance);
                layoutOrderProperty = managerType.GetProperty("WorkLayoutOrder", BindingFlags.Public | BindingFlags.Instance);
                getGroupForWorkGiverMethod = managerType.GetMethod("GetGroupForWorkGiver", BindingFlags.Public | BindingFlags.Instance);
                isAssignedToCustomGroupMethod = managerType.GetMethod("IsAssignedToCustomGroup", BindingFlags.Public | BindingFlags.Instance);
                getAssignedWorkGiverAssignmentsMethod = managerType.GetMethod("GetAssignedWorkGiverAssignments", BindingFlags.Public | BindingFlags.Instance);

                Type groupType = GenTypes.GetTypeInAnyAssembly("WorkTabGroups.MajorWorkGroupData");
                Type layoutEntryType = GenTypes.GetTypeInAnyAssembly("WorkTabGroups.WorkLayoutEntry");
                Type layoutKindType = GenTypes.GetTypeInAnyAssembly(LayoutEntryKindTypeName);
                if (groupType == null || layoutEntryType == null || layoutKindType == null)
                {
                    return false;
                }

                groupDefNameField = groupType.GetField("defName", BindingFlags.Public | BindingFlags.Instance);
                groupLabelField = groupType.GetField("label", BindingFlags.Public | BindingFlags.Instance);
                groupAssignedNamesField = groupType.GetField("assignedWorkGiverDefNames", BindingFlags.Public | BindingFlags.Instance);
                layoutEntryKeyField = layoutEntryType.GetField("key", BindingFlags.Public | BindingFlags.Instance);
                layoutEntryKindField = layoutEntryType.GetField("kind", BindingFlags.Public | BindingFlags.Instance);
                customGroupKindValue = Enum.Parse(layoutKindType, "CustomGroup");

                reflectionAvailable = instanceProperty != null
                    && groupsProperty != null
                    && layoutOrderProperty != null
                    && getGroupForWorkGiverMethod != null
                    && isAssignedToCustomGroupMethod != null
                    && getAssignedWorkGiverAssignmentsMethod != null
                    && groupDefNameField != null
                    && groupLabelField != null
                    && groupAssignedNamesField != null
                    && layoutEntryKeyField != null
                    && layoutEntryKindField != null
                    && customGroupKindValue != null;
            }
            catch (Exception ex)
            {
                Log.Warning("[WorkMonitor] WorkTabGroups integration unavailable: " + ex.Message);
                reflectionAvailable = false;
            }

            return reflectionAvailable;
        }

        private static WorkTabCustomGroupSnapshot MapCustomGroup(object group)
        {
            if (group == null)
            {
                return default;
            }

            string defName = ReadGroupDefName(group);
            if (defName.NullOrEmpty())
            {
                return default;
            }

            string label = groupLabelField.GetValue(group) as string;
            List<string> assignedNames = new List<string>();
            if (groupAssignedNamesField.GetValue(group) is IEnumerable names)
            {
                foreach (object name in names)
                {
                    if (name is string wgName && !wgName.NullOrEmpty())
                    {
                        assignedNames.Add(wgName);
                    }
                }
            }

            return new WorkTabCustomGroupSnapshot(defName, label, assignedNames);
        }

        private static string ReadGroupDefName(object group)
        {
            return group == null ? null : groupDefNameField?.GetValue(group) as string;
        }

        private static object ReadKeyValuePairKey(object pair)
        {
            if (pair == null)
            {
                return null;
            }

            Type pairType = pair.GetType();
            PropertyInfo keyProperty = pairType.GetProperty("Key");
            return keyProperty?.GetValue(pair);
        }
    }
}
