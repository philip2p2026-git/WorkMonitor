using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using WorkMonitor.Groups;

namespace WorkMonitor.UI
{
    public enum ColonistDetailView
    {
        GroupsSummary,
        GroupWorkDetail
    }

    public class ColonistDetailPanel
    {
        private const float RowHeight = 24f;
        private const float ColonistDropdownWidth = 160f;

        private Vector2 scroll;
        private ColonistStats stats;
        private ColonistGroupWorkDetail groupDetail;
        private ColonistDetailView view = ColonistDetailView.GroupsSummary;
        private WorkGroupSnapshot selectedGroup;
        private Pawn pendingColonistSelection;

        public void SetColonist(Pawn pawn, WorkGroupSnapshot initialGroup = null, bool openGroupDetail = false)
        {
            stats = ColonistStatsAggregator.Build(pawn);
            selectedGroup = initialGroup;
            groupDetail = null;

            if (openGroupDetail && initialGroup != null)
            {
                view = ColonistDetailView.GroupWorkDetail;
                groupDetail = ColonistStatsAggregator.BuildGroupDetail(pawn, initialGroup);
            }
            else
            {
                view = ColonistDetailView.GroupsSummary;
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
            string backLabel = view == ColonistDetailView.GroupWorkDetail
                ? "WorkMonitor.BackToGroups".Translate()
                : "WorkMonitor.Back".Translate();
            if (Widgets.ButtonText(new Rect(rect.x, rect.y, 90f, 26f), backLabel))
            {
                if (view == ColonistDetailView.GroupWorkDetail)
                {
                    view = ColonistDetailView.GroupsSummary;
                    groupDetail = null;
                    return;
                }

                back = true;
                return;
            }

            Rect dropdownRect = new Rect(rect.x + 96f, rect.y, ColonistDropdownWidth, 26f);
            List<FloatMenuOption> colonistOptions = WorkMonitorDropdownUtility.BuildOptions(
                WorkMonitorUtility.MonitorColonists(),
                p => p.LabelShort,
                p => pendingColonistSelection = p);
            WorkMonitorDropdownUtility.DrawDropdown(dropdownRect, stats.Label, colonistOptions);

            if (view == ColonistDetailView.GroupWorkDetail && selectedGroup != null)
            {
                Widgets.Label(
                    new Rect(dropdownRect.xMax + 8f, rect.y + 2f, rect.width - dropdownRect.xMax - 16f, 22f),
                    "WorkMonitor.ColonistWorkDetailTitle".Translate(stats.Label, selectedGroup.Label));
            }

            Rect header = new Rect(rect.x, rect.y + 32f, rect.width, 18f);
            DrawHeader(header);

            Rect content = new Rect(rect.x, header.yMax + 8f, rect.width, rect.yMax - header.yMax - 12f);

            if (view == ColonistDetailView.GroupsSummary)
            {
                float viewHeight = RowHeight * 2f + stats.GroupStats.Count * RowHeight;
                Rect viewRect = new Rect(0f, 0f, content.width - 16f, viewHeight);
                Widgets.BeginScrollView(content, ref scroll, viewRect);
                float y = 0f;
                DrawGroupTable(new Rect(0f, y, viewRect.width, viewHeight), ref y);
                Widgets.EndScrollView();
            }
            else
            {
                int rowCount = groupDetail?.WorkGiverStats.Count ?? 0;
                float viewHeight = RowHeight * 2f + rowCount * RowHeight;
                Rect viewRect = new Rect(0f, 0f, content.width - 16f, Mathf.Max(viewHeight, content.height));
                Widgets.BeginScrollView(content, ref scroll, viewRect);
                float y = 0f;
                DrawWorkGiverTable(new Rect(0f, y, viewRect.width, viewHeight), ref y);
                Widgets.EndScrollView();
            }

            if (pendingColonistSelection != null)
            {
                Pawn pawn = pendingColonistSelection;
                pendingColonistSelection = null;
                WorkGroupSnapshot group = view == ColonistDetailView.GroupWorkDetail ? selectedGroup : null;
                bool openDetail = view == ColonistDetailView.GroupWorkDetail && group != null;
                SetColonist(pawn, group, openDetail);
            }
        }

        private void DrawHeader(Rect rect)
        {
            Text.Font = GameFont.Tiny;
            string passion = WorkMonitorUiUtility.PassionShort(stats.TopPassion);
            bool showHours = WorkMonitorMod.Settings?.showTimeInHours ?? true;

            if (view == ColonistDetailView.GroupWorkDetail && groupDetail != null)
            {
                string summary = "WorkMonitor.ColonistGroupSummary".Translate(
                    passion,
                    groupDetail.JobCount,
                    WorkMonitorUtility.FormatWorkUnits(groupDetail.WorkUnitsSpent),
                    WorkMonitorUtility.FormatDuration(groupDetail.TravelTicksSpent, showHours),
                    WorkMonitorUtility.FormatDuration(groupDetail.WorkTicksSpent, showHours));
                Widgets.Label(new Rect(rect.x, rect.y, rect.width, 16f), summary);
                return;
            }

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
                Rect row = new Rect(area.x, y, area.width, RowHeight);
                if (rowIndex % 2 == 1)
                {
                    Widgets.DrawBoxSolid(row, new Color(1f, 1f, 1f, 0.03f));
                }

                if (Widgets.ButtonInvisible(row))
                {
                    selectedGroup = groupStat.Group;
                    groupDetail = ColonistStatsAggregator.BuildGroupDetail(stats.Pawn, selectedGroup);
                    view = ColonistDetailView.GroupWorkDetail;
                    scroll = Vector2.zero;
                }

                if (selectedGroup != null && groupStat.Group.Key.StorageKey == selectedGroup.Key.StorageKey && view == ColonistDetailView.GroupsSummary)
                {
                    Widgets.DrawBoxSolid(row, new Color(0.4f, 0.6f, 1f, 0.08f));
                }

                DrawGroupRow(row, groupStat);
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
            Widgets.Label(new Rect(row.x, row.y, metricsLeft - row.x - 8f, row.height), "WorkMonitor.Group".Translate());
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
            Widgets.Label(new Rect(row.x, row.y, metricsLeft - row.x - 8f, row.height), groupStat.Group.Label.Truncate(metricsLeft - row.x - 8f));
            WorkMonitorTableColumns.GetColonistGroupColumns(row, out Rect jobCol, out Rect workCol, out Rect walkCol, out Rect activeWorkCol, out Rect shareCol);
            bool showHours = WorkMonitorMod.Settings?.showTimeInHours ?? true;

            LabelRight(jobCol, groupStat.JobCount.ToString());
            LabelRight(workCol, WorkMonitorUtility.FormatWorkUnits(groupStat.WorkUnitsSpent));
            LabelRight(walkCol, WorkMonitorUtility.FormatDuration(groupStat.TravelTicksSpent, showHours));
            LabelRight(activeWorkCol, WorkMonitorUtility.FormatDuration(groupStat.WorkTicksSpent, showHours));
            LabelRight(shareCol, WorkMonitorUiUtility.FormatTimeShare(groupStat.TicksSpent, groupStat.GroupTicksSpent));
        }

        private void DrawWorkGiverTable(Rect area, ref float y)
        {
            if (groupDetail == null)
            {
                return;
            }

            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(area.x, y, area.width, 22f), "WorkMonitor.WorkGiversColonist".Translate());
            y += RowHeight;

            DrawWorkGiverHeader(new Rect(area.x, y, area.width, RowHeight));
            y += RowHeight;

            int rowIndex = 0;
            bool showHours = WorkMonitorMod.Settings?.showTimeInHours ?? true;
            foreach (ColonistWorkGiverStat wg in groupDetail.WorkGiverStats)
            {
                Rect row = new Rect(area.x, y, area.width, RowHeight);
                if (rowIndex % 2 == 1)
                {
                    Widgets.DrawBoxSolid(row, new Color(1f, 1f, 1f, 0.03f));
                }

                float metricsLeft = WorkMonitorTableColumns.ColonistWorkGiverMetricsLeftEdge(row);
                Widgets.Label(new Rect(row.x, row.y, metricsLeft - row.x - 8f, row.height), wg.Label.Truncate(metricsLeft - row.x - 8f));
                WorkMonitorTableColumns.GetColonistWorkGiverColumns(row, out Rect jobCol, out Rect workCol, out Rect walkCol, out Rect activeWorkCol);

                LabelRight(jobCol, wg.JobCount.ToString());
                LabelRight(workCol, WorkMonitorUtility.FormatWorkUnits(wg.WorkUnitsSpent));
                LabelRight(walkCol, WorkMonitorUtility.FormatDuration(wg.TravelTicksSpent, showHours));
                LabelRight(activeWorkCol, WorkMonitorUtility.FormatDuration(wg.WorkTicksSpent, showHours));

                y += RowHeight;
                rowIndex++;
            }
        }

        private static void DrawWorkGiverHeader(Rect row)
        {
            Text.Font = GameFont.Tiny;
            Color prev = GUI.color;
            GUI.color = new Color(0.72f, 0.72f, 0.72f);

            float metricsLeft = WorkMonitorTableColumns.ColonistWorkGiverMetricsLeftEdge(row);
            Widgets.Label(new Rect(row.x, row.y, metricsLeft - row.x - 8f, row.height), "WorkMonitor.WorkGiver".Translate());
            WorkMonitorTableColumns.GetColonistWorkGiverColumns(row, out Rect jobCol, out Rect workCol, out Rect walkCol, out Rect activeWorkCol);
            LabelRight(jobCol, "WorkMonitor.Jobs".Translate());
            LabelRight(workCol, "WorkMonitor.Work".Translate());
            LabelRight(walkCol, "WorkMonitor.Walk".Translate());
            LabelRight(activeWorkCol, "WorkMonitor.WorkTime".Translate());

            GUI.color = prev;
        }

        private static void LabelRight(Rect rect, string text)
        {
            Text.Anchor = TextAnchor.MiddleRight;
            Widgets.Label(rect, text);
            Text.Anchor = TextAnchor.UpperLeft;
        }
    }
}
