using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;
using WorkTab;
using WorkTabGroups;

namespace WorkMonitor.Groups
{
    public static class WorkGroupOrderUtility
    {
        public static List<WorkGroupSnapshot> Sort(IEnumerable<WorkGroupSnapshot> groups)
        {
            Dictionary<string, int> order = BuildWorkTabOrderMap();
            return groups
                .OrderBy(g => g.Key.Kind == WorkGroupKind.Other ? int.MaxValue : 0)
                .ThenBy(g => order.TryGetValue(g.Key.StorageKey, out int index) ? index : int.MaxValue - 1)
                .ThenByDescending(g => g.PrimaryWorkType?.naturalPriority ?? -1)
                .ThenBy(g => g.Label)
                .ToList();
        }

        private static Dictionary<string, int> BuildWorkTabOrderMap()
        {
            if (TryBuildOrderFromLayout(out Dictionary<string, int> layoutOrder))
            {
                return layoutOrder;
            }

            return BuildOrderFromWorkColumns(PawnTableDefOf.Work?.columns);
        }

        private static bool TryBuildOrderFromLayout(out Dictionary<string, int> order)
        {
            order = new Dictionary<string, int>();
            IReadOnlyList<WorkLayoutEntry> layoutOrder = WorkTabGroupsProvider.GetWorkLayoutOrder();
            if (layoutOrder == null || layoutOrder.Count == 0)
            {
                return false;
            }

            int index = 0;
            foreach (WorkLayoutEntry entry in layoutOrder)
            {
                if (entry == null || entry.key.NullOrEmpty())
                {
                    continue;
                }

                if (entry.kind == WorkLayoutEntryKind.CustomGroup)
                {
                    string storageKey = WorkGroupKey.ForCustomGroup(entry.key).StorageKey;
                    if (!order.ContainsKey(storageKey))
                    {
                        order[storageKey] = index++;
                    }

                    continue;
                }

                WorkTypeDef workType = DefDatabase<WorkTypeDef>.GetNamedSilentFail(entry.key);
                if (workType == null)
                {
                    continue;
                }

                string workTypeKey = WorkGroupKey.ForWorkType(workType).StorageKey;
                if (!order.ContainsKey(workTypeKey))
                {
                    order[workTypeKey] = index++;
                }
            }

            return order.Count > 0;
        }

        private static Dictionary<string, int> BuildOrderFromWorkColumns(List<PawnColumnDef> columns)
        {
            Dictionary<string, int> order = new Dictionary<string, int>();
            if (columns == null)
            {
                return order;
            }

            int index = 0;
            foreach (PawnColumnDef column in columns)
            {
                if (column.Worker is PawnColumnWorker_WorkType && column.workType != null)
                {
                    order[WorkGroupKey.ForWorkType(column.workType).StorageKey] = index++;
                    continue;
                }

                if (column.Worker is PawnColumnWorker_MajorWorkGroup groupWorker)
                {
                    string defName = groupWorker.BoundGroup?.defName;
                    if (!defName.NullOrEmpty())
                    {
                        order[WorkGroupKey.ForCustomGroup(defName).StorageKey] = index++;
                    }

                    continue;
                }

                if (column.workerClass == null || !column.workerClass.FullName.Contains("PawnColumnWorker_MajorWorkGroup"))
                {
                    continue;
                }

                PawnColumnWorker worker = column.Worker;
                if (worker == null)
                {
                    continue;
                }

                object boundGroup = worker.GetType().GetProperty("BoundGroup")?.GetValue(worker);
                string defNameFallback = boundGroup?.GetType().GetField("defName")?.GetValue(boundGroup) as string;
                if (!defNameFallback.NullOrEmpty())
                {
                    order[WorkGroupKey.ForCustomGroup(defNameFallback).StorageKey] = index++;
                }
            }

            return order;
        }
    }
}
