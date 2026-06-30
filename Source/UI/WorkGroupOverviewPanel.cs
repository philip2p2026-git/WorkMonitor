using System.Collections.Generic;
using UnityEngine;
using Verse;
using WorkMonitor.Groups;

namespace WorkMonitor.UI
{
    public class WorkGroupOverviewPanel
    {
        private const float RowHeight = 30f;
        private const float StatusWidth = 22f;

        private Vector2 scroll;
        private List<WorkGroupStats> cachedStats = new List<WorkGroupStats>();
        private int lastRefreshTick;
        private MonitorRangeState boundRangeState;

        public void RefreshIfNeeded(MonitorRangeState rangeState, bool force = false)
        {
            int refresh = WorkMonitorMod.Settings?.refreshIntervalTicks ?? 60;
            if (!force && Find.TickManager.TicksGame - lastRefreshTick < refresh)
            {
                return;
            }

            boundRangeState = rangeState;
            cachedStats = WorkGroupStatsAggregator.BuildAll(rangeState.RangeHours);
            lastRefreshTick = Find.TickManager.TicksGame;
        }

        public WorkGroupSnapshot Draw(Rect rect, MonitorRangeState rangeState, out bool clicked)
        {
            clicked = false;
            if (boundRangeState != rangeState)
            {
                RefreshIfNeeded(rangeState, force: true);
            }
            else
            {
                RefreshIfNeeded(rangeState);
            }

            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(rect.x, rect.y, rect.width - 200f, 22f), "WorkMonitor.OverviewTitle".Translate());

            Text.Font = GameFont.Tiny;
            WorkMonitorDropdownUtility.DrawRangeDropdown(
                new Rect(rect.xMax - 196f, rect.y, 110f, 24f),
                rangeState,
                () => RefreshIfNeeded(rangeState, force: true));

            if (Widgets.ButtonText(new Rect(rect.xMax - 96f, rect.y, 90f, 24f), "WorkMonitor.Refresh".Translate()))
            {
                RefreshIfNeeded(rangeState, force: true);
            }

            Rect header = new Rect(rect.x, rect.y + 28f, rect.width, 20f);
            DrawHeader(header);

            Rect listRect = new Rect(rect.x, rect.y + 52f, rect.width, rect.height - 92f);
            Rect viewRect = new Rect(0f, 0f, listRect.width - 16f, cachedStats.Count * RowHeight);
            Widgets.BeginScrollView(listRect, ref scroll, viewRect);

            WorkGroupSnapshot selected = null;
            float y = 0f;
            int rowIndex = 0;
            foreach (WorkGroupStats stats in cachedStats)
            {
                Rect row = new Rect(0f, y, viewRect.width, RowHeight);
                if (Widgets.ButtonInvisible(row))
                {
                    clicked = true;
                    selected = stats.Group;
                }

                WorkMonitorUiUtility.DrawRowBackground(row, MonitorRowKind.WorkType, rowIndex);

                DrawRow(row, stats);
                y += RowHeight;
                rowIndex++;
            }

            Widgets.EndScrollView();

            int totalOpenTasks = 0;
            int totalNewTodayTasks = 0;
            float totalMapWork = 0f;
            float totalNewTodayWork = 0f;
            int totalJobs = 0;
            float totalWork = 0f;
            foreach (WorkGroupStats s in cachedStats)
            {
                totalOpenTasks += s.TotalMapOpenTasks;
                totalNewTodayTasks += s.TotalMapNewTodayOpenTasks;
                totalMapWork += s.TotalMapWorkLeft;
                totalNewTodayWork += s.TotalMapNewTodayWorkLeft;
                totalJobs += s.TotalJobCount;
                totalWork += s.TotalWorkUnits;
            }

            Widgets.Label(
                new Rect(rect.x, rect.yMax - 28f, rect.width, 24f),
                "WorkMonitor.FooterSummary".Translate(
                    cachedStats.Count,
                    WorkMonitorUiUtility.FormatMapOpenTasks(totalOpenTasks, totalNewTodayTasks),
                    WorkMonitorUiUtility.FormatMapWorkLeft(totalMapWork, totalNewTodayWork),
                    totalJobs,
                    WorkMonitorUtility.FormatWorkUnits(totalWork)));

            return selected;
        }

        private static void DrawHeader(Rect rect)
        {
            Text.Font = GameFont.Tiny;
            Color prev = GUI.color;
            GUI.color = new Color(0.72f, 0.72f, 0.72f);

            float metricsLeft = WorkMonitorTableColumns.OverviewMetricsLeftEdge(rect);
            Widgets.Label(new Rect(rect.x, rect.y, StatusWidth, rect.height), "WorkMonitor.Status".Translate());
            Widgets.Label(new Rect(rect.x + StatusWidth + 4f, rect.y, metricsLeft - rect.x - StatusWidth - 12f, rect.height), "WorkMonitor.Group".Translate());
            WorkMonitorTableColumns.DrawOverviewMetricHeader(rect, LabelRight);

            GUI.color = prev;
        }

        private static void DrawRow(Rect row, WorkGroupStats stats)
        {
            Text.Font = GameFont.Small;
            float metricsLeft = WorkMonitorTableColumns.OverviewMetricsLeftEdge(row);

            Color dot = WorkMonitorUiUtility.StatusColor(stats.Status);
            float dotSize = 10f;
            Widgets.DrawBoxSolid(
                new Rect(row.x + (StatusWidth - dotSize) * 0.5f, row.y + (row.height - dotSize) * 0.5f, dotSize, dotSize),
                dot);

            Widgets.Label(
                new Rect(row.x + StatusWidth + 4f, row.y, metricsLeft - row.x - StatusWidth - 12f, row.height),
                stats.Group.Label.Truncate(metricsLeft - row.x - StatusWidth - 12f));

            WorkMonitorTableColumns.GetOverviewMetricColumns(row, out Rect existJobCol, out Rect existWorkCol, out Rect jobProcessedCol, out Rect workProcessedCol);
            string jobsText = WorkMonitorUiUtility.FormatMapOpenTasks(stats.TotalMapOpenTasks, stats.TotalMapNewTodayOpenTasks);
            string workText = WorkMonitorUiUtility.FormatMapWorkLeft(stats.TotalMapWorkLeft, stats.TotalMapNewTodayWorkLeft);
            LabelRight(existJobCol, jobsText);
            TooltipHandler.TipRegion(existJobCol, "WorkMonitor.MapJobsNewTodayTip".Translate(jobsText));
            LabelRight(existWorkCol, workText);
            TooltipHandler.TipRegion(existWorkCol, "WorkMonitor.MapWorkNewTodayTip".Translate(workText));
            LabelRight(jobProcessedCol, stats.TotalJobCount.ToString());
            LabelRight(workProcessedCol, WorkMonitorUtility.FormatWorkUnits(stats.TotalWorkUnits));
        }

        private static void LabelRight(Rect rect, string text)
        {
            Text.Anchor = TextAnchor.MiddleRight;
            Widgets.Label(rect, text);
            Text.Anchor = TextAnchor.UpperLeft;
        }
    }
}
