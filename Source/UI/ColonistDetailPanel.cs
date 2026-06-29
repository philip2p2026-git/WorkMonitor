using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using WorkMonitor.Groups;

namespace WorkMonitor.UI
{
    public class ColonistDetailPanel
    {
        private const float RowHeight = 24f;
        private const float ColonistDropdownWidth = 160f;
        private const float ExpandButtonWidth = 20f;
        private const float WorkGiverIndent = 16f;

        private Vector2 scroll;
        private ColonistStats stats;
        private readonly HashSet<string> expandedGroupKeys = new HashSet<string>();
        private readonly Dictionary<string, ColonistGroupWorkDetail> groupDetailCache = new Dictionary<string, ColonistGroupWorkDetail>();
        private Pawn pendingColonistSelection;

        public void SetColonist(Pawn pawn, WorkGroupSnapshot initialGroup = null, bool openGroupDetail = false)
        {
            stats = ColonistStatsAggregator.Build(pawn);
            expandedGroupKeys.Clear();
            groupDetailCache.Clear();

            if (openGroupDetail && initialGroup != null)
            {
                expandedGroupKeys.Add(initialGroup.Key.StorageKey);
            }
        }

        public void Draw(Rect rect, out bool back)
        {
            back = false;

            if (stats?.Pawn == null)
            {
                return;
            }

            Text.Font = GameFont.Small;
            if (Widgets.ButtonText(new Rect(rect.x, rect.y, 90f, 26f), "WorkMonitor.Back".Translate()))
            {
                back = true;
                return;
            }

            Rect dropdownRect = new Rect(rect.x + 96f, rect.y, ColonistDropdownWidth, 26f);
            List<FloatMenuOption> colonistOptions = WorkMonitorDropdownUtility.BuildOptions(
                WorkMonitorUtility.MonitorColonists(),
                p => p.LabelShort,
                p => pendingColonistSelection = p);
            WorkMonitorDropdownUtility.DrawDropdown(dropdownRect, stats.Label, colonistOptions);

            Rect header = new Rect(rect.x, rect.y + 32f, rect.width, 18f);
            DrawHeader(header);

            Rect content = new Rect(rect.x, header.yMax + 8f, rect.width, rect.yMax - header.yMax - 12f);
            float viewHeight = CalculateGroupsViewHeight();
            Rect viewRect = new Rect(0f, 0f, content.width - 16f, viewHeight);
            Widgets.BeginScrollView(content, ref scroll, viewRect);
            float y = 0f;
            DrawGroupTable(new Rect(0f, y, viewRect.width, viewHeight), ref y);
            Widgets.EndScrollView();

            if (pendingColonistSelection != null)
            {
                HashSet<string> preservedExpansion = new HashSet<string>(expandedGroupKeys);
                Pawn pawn = pendingColonistSelection;
                pendingColonistSelection = null;
                SetColonist(pawn);
                foreach (string key in preservedExpansion)
                {
                    expandedGroupKeys.Add(key);
                }
            }
        }

        private float CalculateGroupsViewHeight()
        {
            float height = RowHeight * 2f;
            foreach (ColonistGroupStat groupStat in stats.GroupStats)
            {
                height += RowHeight;
                if (expandedGroupKeys.Contains(groupStat.Group.Key.StorageKey))
                {
                    ColonistGroupWorkDetail detail = GetGroupDetail(groupStat.Group);
                    height += detail.WorkGiverStats.Count * RowHeight;
                }
            }

            return height;
        }

        private ColonistGroupWorkDetail GetGroupDetail(WorkGroupSnapshot group)
        {
            string key = group.Key.StorageKey;
            if (!groupDetailCache.TryGetValue(key, out ColonistGroupWorkDetail detail))
            {
                detail = ColonistStatsAggregator.BuildGroupDetail(stats.Pawn, group);
                groupDetailCache[key] = detail;
            }

            return detail;
        }

        private void DrawHeader(Rect rect)
        {
            Text.Font = GameFont.Tiny;
            string passion = WorkMonitorUiUtility.PassionShort(stats.TopPassion);
            bool showHours = WorkMonitorMod.Settings?.showTimeInHours ?? true;

            string totalSummary = "WorkMonitor.ColonistSummaryWalkWork".Translate(
                passion,
                stats.TotalJobCount,
                WorkMonitorUtility.FormatWorkUnits(stats.TotalWorkUnits),
                WorkMonitorUtility.FormatDuration(stats.TotalTravelTicksSpent, showHours),
                WorkMonitorUtility.FormatDuration(stats.TotalWorkTicksSpent, showHours));
            Widgets.Label(new Rect(rect.x, rect.y, rect.width, 16f), totalSummary);
        }

        private void DrawGroupTable(Rect area, ref float y)
        {
            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(area.x, y, area.width, 22f), "WorkMonitor.Groups".Translate());
            y += RowHeight;

            DrawGroupHeader(new Rect(area.x, y, area.width, RowHeight));
            y += RowHeight;

            int rowIndex = 0;
            foreach (ColonistGroupStat groupStat in stats.GroupStats)
            {
                string storageKey = groupStat.Group.Key.StorageKey;
                bool expanded = expandedGroupKeys.Contains(storageKey);

                Rect row = new Rect(area.x, y, area.width, RowHeight);
                if (rowIndex % 2 == 1)
                {
                    Widgets.DrawBoxSolid(row, new Color(1f, 1f, 1f, 0.03f));
                }

                Rect expandRect = new Rect(row.x, row.y, ExpandButtonWidth, row.height);
                if (Widgets.ButtonText(expandRect, expanded ? "▼" : "▶"))
                {
                    if (expanded)
                    {
                        expandedGroupKeys.Remove(storageKey);
                    }
                    else
                    {
                        expandedGroupKeys.Add(storageKey);
                    }
                }

                DrawGroupRow(row, groupStat);
                y += RowHeight;
                rowIndex++;

                if (expanded)
                {
                    DrawExpandedWorkGivers(area, groupStat.Group, ref y, ref rowIndex);
                }
            }
        }

        private void DrawExpandedWorkGivers(Rect area, WorkGroupSnapshot group, ref float y, ref int rowIndex)
        {
            ColonistGroupWorkDetail detail = GetGroupDetail(group);
            bool showHours = WorkMonitorMod.Settings?.showTimeInHours ?? true;
            Rect columnRow = new Rect(area.x, y, area.width, RowHeight);

            foreach (ColonistWorkGiverStat wg in detail.WorkGiverStats)
            {
                Rect row = new Rect(area.x + WorkGiverIndent, y, area.width - WorkGiverIndent, RowHeight);
                if (rowIndex % 2 == 1)
                {
                    Widgets.DrawBoxSolid(row, new Color(1f, 1f, 1f, 0.02f));
                }

                columnRow.y = y;
                float metricsLeft = WorkMonitorTableColumns.ColonistGroupMetricsLeftEdge(columnRow);
                Text.Font = GameFont.Tiny;
                Color prev = GUI.color;
                GUI.color = new Color(0.8f, 0.8f, 0.8f);
                Widgets.Label(new Rect(area.x + WorkGiverIndent, row.y, metricsLeft - area.x - WorkGiverIndent - 8f, row.height), wg.Label.Truncate(metricsLeft - area.x - WorkGiverIndent - 8f));
                GUI.color = prev;
                Text.Font = GameFont.Small;

                WorkMonitorTableColumns.GetColonistGroupColumns(columnRow, out Rect jobCol, out Rect workCol, out Rect walkCol, out Rect activeWorkCol, out _);
                LabelRight(jobCol, wg.JobCount.ToString());
                LabelRight(workCol, WorkMonitorUtility.FormatWorkUnits(wg.WorkUnitsSpent));
                LabelRight(walkCol, WorkMonitorUtility.FormatDuration(wg.TravelTicksSpent, showHours));
                LabelRight(activeWorkCol, WorkMonitorUtility.FormatDuration(wg.WorkTicksSpent, showHours));

                y += RowHeight;
                rowIndex++;
            }
        }

        private static void DrawGroupHeader(Rect row)
        {
            Text.Font = GameFont.Tiny;
            Color prev = GUI.color;
            GUI.color = new Color(0.72f, 0.72f, 0.72f);

            float metricsLeft = WorkMonitorTableColumns.ColonistGroupMetricsLeftEdge(row);
            Widgets.Label(new Rect(row.x + ExpandButtonWidth, row.y, metricsLeft - row.x - ExpandButtonWidth - 8f, row.height), "WorkMonitor.Group".Translate());
            WorkMonitorTableColumns.GetColonistGroupColumns(row, out Rect jobCol, out Rect workCol, out Rect walkCol, out Rect activeWorkCol, out Rect shareCol);
            LabelRight(jobCol, "WorkMonitor.Jobs".Translate());
            LabelRight(workCol, "WorkMonitor.Work".Translate());
            LabelRight(walkCol, "WorkMonitor.Walk".Translate());
            LabelRight(activeWorkCol, "WorkMonitor.WorkTime".Translate());
            LabelRight(shareCol, "WorkMonitor.TimeShare".Translate());

            GUI.color = prev;
        }

        private static void DrawGroupRow(Rect row, ColonistGroupStat groupStat)
        {
            Text.Font = GameFont.Small;
            float metricsLeft = WorkMonitorTableColumns.ColonistGroupMetricsLeftEdge(row);
            Widgets.Label(new Rect(row.x + ExpandButtonWidth, row.y, metricsLeft - row.x - ExpandButtonWidth - 8f, row.height), groupStat.Group.Label.Truncate(metricsLeft - row.x - ExpandButtonWidth - 8f));
            WorkMonitorTableColumns.GetColonistGroupColumns(row, out Rect jobCol, out Rect workCol, out Rect walkCol, out Rect activeWorkCol, out Rect shareCol);
            bool showHours = WorkMonitorMod.Settings?.showTimeInHours ?? true;

            LabelRight(jobCol, groupStat.JobCount.ToString());
            LabelRight(workCol, WorkMonitorUtility.FormatWorkUnits(groupStat.WorkUnitsSpent));
            LabelRight(walkCol, WorkMonitorUtility.FormatDuration(groupStat.TravelTicksSpent, showHours));
            LabelRight(activeWorkCol, WorkMonitorUtility.FormatDuration(groupStat.WorkTicksSpent, showHours));
            LabelRight(shareCol, WorkMonitorUiUtility.FormatTimeShare(groupStat.TicksSpent, groupStat.GroupTicksSpent));
        }

        private static void LabelRight(Rect rect, string text)
        {
            Text.Anchor = TextAnchor.MiddleRight;
            Widgets.Label(rect, text);
            Text.Anchor = TextAnchor.UpperLeft;
        }
    }
}
