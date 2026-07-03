using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace WorkMonitor.Groups
{
    /// <summary>
    /// Cached WorkGiver → monitor-row storage keys. Mirrors WorkTypeGroupProvider,
    /// WorkTabGroupsProvider, and OtherWorkGroupProvider assignment rules.
    /// </summary>
    public static class WorkGiverAssignmentIndex
    {
        private const int CacheTicks = 250;

        private static Dictionary<WorkGiverDef, string[]> keysByWorkGiver;
        private static HashSet<WorkGiverDef> assignedWorkGivers;
        private static int lastCacheTick = -1;

        public static void EnsureFresh(bool forceRefresh = false)
        {
            int tick = Find.TickManager?.TicksGame ?? 0;
            if (!forceRefresh && keysByWorkGiver != null && tick - lastCacheTick < CacheTicks)
            {
                return;
            }

            Rebuild(tick);
        }

        public static void Rebuild(int tick)
        {
            var builder = new Dictionary<WorkGiverDef, List<string>>();
            var assigned = new HashSet<WorkGiverDef>();

            foreach (WorkGiverDef wg in WorkTabGroupsIntegration.EnumerateCustomAssignedWorkGivers())
            {
                if (wg == null)
                {
                    continue;
                }

                string customKey = WorkTabGroupsIntegration.GetCustomGroupStorageKeyForWorkGiver(wg);
                if (customKey.NullOrEmpty())
                {
                    continue;
                }

                AddKey(builder, wg, customKey);
                assigned.Add(wg);
            }

            foreach (WorkTypeDef workType in DefDatabase<WorkTypeDef>.AllDefsListForReading)
            {
                if (workType.workGiversByPriority == null)
                {
                    continue;
                }

                string workTypeKey = WorkGroupKey.ForWorkType(workType).StorageKey;
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

                    AddKey(builder, wg, workTypeKey);
                    assigned.Add(wg);
                }
            }

            string otherKey = WorkGroupKey.ForOther().StorageKey;
            foreach (WorkGiverDef wg in DefDatabase<WorkGiverDef>.AllDefsListForReading)
            {
                if (wg == null)
                {
                    continue;
                }

                if (!builder.ContainsKey(wg))
                {
                    AddKey(builder, wg, otherKey);
                }
            }

            keysByWorkGiver = builder.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.ToArray());
            assignedWorkGivers = assigned;
            lastCacheTick = tick;
        }

        public static IReadOnlyList<string> GetStorageKeysForWorkGiver(WorkGiverDef workGiver)
        {
            if (workGiver == null)
            {
                return Array.Empty<string>();
            }

            EnsureFresh();
            if (keysByWorkGiver == null || !keysByWorkGiver.TryGetValue(workGiver, out string[] keys))
            {
                return Array.Empty<string>();
            }

            return keys;
        }

        public static IReadOnlyList<string> GetStorageKeysForDefName(string workGiverDefName)
        {
            if (workGiverDefName.NullOrEmpty())
            {
                return Array.Empty<string>();
            }

            WorkGiverDef workGiver = DefDatabase<WorkGiverDef>.GetNamedSilentFail(workGiverDefName);
            return GetStorageKeysForWorkGiver(workGiver);
        }

        public static HashSet<WorkGiverDef> GetAssignedWorkGivers()
        {
            EnsureFresh();
            return assignedWorkGivers ?? new HashSet<WorkGiverDef>();
        }

        private static void AddKey(Dictionary<WorkGiverDef, List<string>> builder, WorkGiverDef wg, string key)
        {
            if (!builder.TryGetValue(wg, out List<string> keys))
            {
                keys = new List<string>();
                builder[wg] = keys;
            }

            if (!keys.Contains(key))
            {
                keys.Add(key);
            }
        }
    }
}
