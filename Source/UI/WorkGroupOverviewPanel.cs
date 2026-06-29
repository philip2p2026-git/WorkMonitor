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

        public void RefreshIfNeeded(bool force = false)
        {
            int refresh = WorkMonitorMod.Settings?.refreshIntervalTicks ?? 60;
            if (!force && Find.TickManager.TicksGame - lastRefreshTick < refresh)
            {
                return;
            }

            cachedStats = WorkGroupStatsAggregator.BuildAll();
            lastRefreshTick = Find.TickManager.TicksGame;
        }

        public WorkGroupSnapshot Draw(Rect rect, out bool clicked)
        {
            clicked = false;
            RefreshIfNeeded();

            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(rect.x, rect.y, rect.width, 22f), "WorkMonitor.OverviewTitle".Translate());

            Rect header = new Rect(rect.x, rect.y + 24f, rect.width, 20f);
            DrawHeader(header);

            Rect listRect = new Rect(rect.x, rect.y + 48f, rect.width, rect.height - 88f);
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

                if (rowIndex % 2 == 1)
                {
                    Widgets.DrawBoxSolid(row, new Color(1f, 1f, 1f, 0.03f));
                }

                DrawRow(row, stats);
                y += RowHeight;
                rowIndex++;
            }

            Widgets.EndScrollView();

            int totalOpenTasks = 0;
            float totalMapWork = 0f;
            int totalJobs = 0;
            float totalWork = 0f;
            foreach (WorkGroupStats s in cachedStats)
            {
                totalOpenTasks += s.TotalMapOpenTasks;
                totalMapWork += s.TotalMapWorkLeft;
                totalJobs += s.TotalJobCount;
                totalWork += s.TotalWorkUnits;
            }

            Widgets.Label(
                new Rect(rect.x, rect.yMax - 28f, rect.width, 24f),
                "WorkMonitor.FooterSummary".Translate(
                    cachedStats.Count,
                    totalOpenTasks,
                    WorkMonitorUtility.FormatWorkUnits(totalMapWork),
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
            LabelRight(existJobCol, stats.TotalMapOpenTasks.ToString());
            LabelRight(existWorkCol, WorkMonitorUtility.FormatWorkUnits(stats.TotalMapWorkLeft));
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
