using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;
using WorkTab;

namespace WorkMonitor.Groups
{
    public static class WorkGroupOrderUtility
    {
        public static List<WorkGroupSnapshot> Sort(IEnumerable<WorkGroupSnapshot> groups)
        {
            Dictionary<string, int> order = BuildWorkTabOrderMap();
            return groups
                .OrderBy(g => order.TryGetValue(g.Key.StorageKey, out int index) ? index : int.MaxValue)
                .ThenByDescending(g => g.PrimaryWorkType?.naturalPriority ?? -1)
                .ThenBy(g => g.Label)
                .ToList();
        }

        private static Dictionary<string, int> BuildWorkTabOrderMap()
        {
            Dictionary<string, int> order = new Dictionary<string, int>();
            MainTabWindow_WorkTab workTab = MainTabWindow_WorkTab.Instance;
            PawnTable table = workTab?.Table;
            List<PawnColumnDef> columns = table == null
                ? null
                : Traverse.Create(table).Field("columns").GetValue<List<PawnColumnDef>>();
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
                string defName = boundGroup?.GetType().GetField("defName")?.GetValue(boundGroup) as string;
                if (!defName.NullOrEmpty())
                {
                    order[WorkGroupKey.ForCustomGroup(defName).StorageKey] = index++;
                }
            }

            return order;
        }
    }
}
