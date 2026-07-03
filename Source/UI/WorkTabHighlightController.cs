using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using WorkMonitor.Groups;
using WorkTab;

namespace WorkMonitor.UI
{
    public static class WorkTabHighlightController
    {
        public static void HighlightGroup(WorkGroupSnapshot group)
        {
            if (group == null)
            {
                return;
            }

            MainTabWindow_WorkTab workTab = MainTabWindow_WorkTab.Instance;
            if (workTab == null)
            {
                MainButtonDef workButton = DefDatabase<MainButtonDef>.GetNamedSilentFail("Work");
                if (workButton != null)
                {
                    Find.MainTabsRoot.SetCurrentTab(workButton);
                }

                workTab = MainTabWindow_WorkTab.Instance;
            }

            PawnTable table = Traverse.Create(workTab).Field("table").GetValue<PawnTable>();
            if (table == null)
            {
                return;
            }

            List<PawnColumnDef> columns = Traverse.Create(table).Field("columns").GetValue<List<PawnColumnDef>>();
            if (columns == null)
            {
                return;
            }

            if (group.Key.Kind == WorkGroupKind.WorkType)
            {
                foreach (PawnColumnDef column in columns)
                {
                    if (column.Worker is PawnColumnWorker_WorkType worker
                        && column.workType != null
                        && column.workType.defName == group.Key.Id)
                    {
                        MainTabWindow_WorkTab.Expand(worker, true);
                        return;
                    }
                }
            }
            else if (group.Key.Kind == WorkGroupKind.CustomGroup && WorkTabGroupsIntegration.IsActive)
            {
                foreach (PawnColumnDef column in columns)
                {
                    if (column.Worker is IExpandableColumn expandable
                        && WorkTabGroupsIntegration.TryReadMajorWorkGroupDefName(column, out string defName)
                        && defName == group.Key.Id)
                    {
                        MainTabWindow_WorkTab.Expand(expandable, true);
                        return;
                    }
                }
            }
            else if (group.PrimaryWorkType != null)
            {
                foreach (PawnColumnDef column in columns)
                {
                    if (column.Worker is PawnColumnWorker_WorkType worker
                        && column.workType == group.PrimaryWorkType)
                    {
                        MainTabWindow_WorkTab.Expand(worker, true);
                        return;
                    }
                }
            }
        }
    }
}
