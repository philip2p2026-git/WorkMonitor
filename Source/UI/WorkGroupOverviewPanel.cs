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
        private const float JobsWidth = 42f;
        private const float TimeWidth = 50f;
        private const float WorkedWidth = 52f;
        private const float InterestWidth = 58f;
        private const float ColumnGap = 10f;

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

            int totalJobs = 0;
            int totalTicks = 0;
            foreach (WorkGroupStats s in cachedStats)
            {
                totalJobs += s.TotalJobCount;
                totalTicks += s.TotalTicksSpent;
            }

            string time = WorkMonitorUtility.FormatDuration(totalTicks, WorkMonitorMod.Settings?.showTimeInHours ?? true);
            Widgets.Label(
                new Rect(rect.x, rect.yMax - 28f, rect.width, 24f),
                "WorkMonitor.FooterSummary".Translate(cachedStats.Count, totalJobs, time));

            return selected;
        }

        private static void DrawHeader(Rect rect)
        {
            Text.Font = GameFont.Tiny;
            Color prev = GUI.color;
            GUI.color = new Color(0.72f, 0.72f, 0.72f);

            GetColumnRects(rect, out Rect statusCol, out Rect groupCol, out Rect jobsCol, out Rect timeCol, out Rect workedCol, out Rect interestCol);

            Widgets.Label(statusCol, "WorkMonitor.Status".Translate());
            Widgets.Label(groupCol, "WorkMonitor.Group".Translate());
            LabelRight(jobsCol, "WorkMonitor.Jobs".Translate());
            LabelRight(timeCol, "WorkMonitor.Time".Translate());
            LabelRight(workedCol, "WorkMonitor.Worked".Translate());
            LabelRight(interestCol, "WorkMonitor.Interest".Translate());

            GUI.color = prev;
        }

        private static void DrawRow(Rect row, WorkGroupStats stats)
        {
            Text.Font = GameFont.Small;
            GetColumnRects(row, out Rect statusCol, out Rect groupCol, out Rect jobsCol, out Rect timeCol, out Rect workedCol, out Rect interestCol);

            Color dot = WorkMonitorUiUtility.StatusColor(stats.Status);
            float dotSize = 10f;
            Widgets.DrawBoxSolid(
                new Rect(statusCol.x + (statusCol.width - dotSize) * 0.5f, row.y + (row.height - dotSize) * 0.5f, dotSize, dotSize),
                dot);

            Widgets.Label(groupCol, stats.Group.Label.Truncate(groupCol.width));
            LabelRight(jobsCol, stats.TotalJobCount.ToString());
            LabelRight(timeCol,
                WorkMonitorUtility.FormatDuration(stats.TotalTicksSpent, WorkMonitorMod.Settings?.showTimeInHours ?? true));
            LabelRight(workedCol, stats.WorkedCount + "/" + stats.EnabledCount);
            LabelRight(interestCol, WorkMonitorUiUtility.FormatInterestRatio(stats));
        }

        private static void GetColumnRects(
            Rect row,
            out Rect statusCol,
            out Rect groupCol,
            out Rect jobsCol,
            out Rect timeCol,
            out Rect workedCol,
            out Rect interestCol)
        {
            float interestX = row.xMax - InterestWidth;
            float workedX = interestX - ColumnGap - WorkedWidth;
            float timeX = workedX - ColumnGap - TimeWidth;
            float jobsX = timeX - ColumnGap - JobsWidth;
            float groupX = row.x + StatusWidth + 4f;
            float groupWidth = jobsX - ColumnGap - groupX;

            statusCol = new Rect(row.x, row.y, StatusWidth, row.height);
            groupCol = new Rect(groupX, row.y, Mathf.Max(groupWidth, 60f), row.height);
            jobsCol = new Rect(jobsX, row.y, JobsWidth, row.height);
            timeCol = new Rect(timeX, row.y, TimeWidth, row.height);
            workedCol = new Rect(workedX, row.y, WorkedWidth, row.height);
            interestCol = new Rect(interestX, row.y, InterestWidth, row.height);
        }

        private static void LabelRight(Rect rect, string text)
        {
            Text.Anchor = TextAnchor.MiddleRight;
            Widgets.Label(rect, text);
            Text.Anchor = TextAnchor.UpperLeft;
        }
    }
}
