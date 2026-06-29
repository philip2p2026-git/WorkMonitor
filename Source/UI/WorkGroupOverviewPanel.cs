using System.Collections.Generic;
using UnityEngine;
using Verse;
using WorkMonitor.Groups;

namespace WorkMonitor.UI
{
    public class WorkGroupOverviewPanel
    {
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
            Rect viewRect = new Rect(0f, 0f, listRect.width - 16f, cachedStats.Count * 28f);
            Widgets.BeginScrollView(listRect, ref scroll, viewRect);

            WorkGroupSnapshot selected = null;
            float y = 0f;
            foreach (WorkGroupStats stats in cachedStats)
            {
                Rect row = new Rect(0f, y, viewRect.width, 26f);
                if (Widgets.ButtonInvisible(row))
                {
                    clicked = true;
                    selected = stats.Group;
                }

                DrawRow(row, stats);
                y += 28f;
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
            float x = rect.x;
            Widgets.Label(new Rect(x, rect.y, 36f, rect.height), "WorkMonitor.Status".Translate());
            x += 40f;
            Widgets.Label(new Rect(x, rect.y, 110f, rect.height), "WorkMonitor.Group".Translate());
            x += 115f;
            Widgets.Label(new Rect(x, rect.y, 40f, rect.height), "WorkMonitor.Jobs".Translate());
            x += 45f;
            Widgets.Label(new Rect(x, rect.y, 50f, rect.height), "WorkMonitor.Time".Translate());
            x += 55f;
            Widgets.Label(new Rect(x, rect.y, 55f, rect.height), "WorkMonitor.Worked".Translate());
            x += 60f;
            Widgets.Label(new Rect(x, rect.y, 80f, rect.height), "WorkMonitor.Interest".Translate());
        }

        private static void DrawRow(Rect row, WorkGroupStats stats)
        {
            Text.Font = GameFont.Small;
            float x = row.x + 4f;
            Color dot = WorkMonitorUiUtility.StatusColor(stats.Status);
            Widgets.DrawBoxSolid(new Rect(x, row.y + 8f, 10f, 10f), dot);
            x += 18f;
            Widgets.Label(new Rect(x, row.y, 105f, row.height), stats.Group.Label);
            x += 110f;
            Widgets.Label(new Rect(x, row.y, 40f, row.height), stats.TotalJobCount.ToString());
            x += 45f;
            Widgets.Label(new Rect(x, row.y, 50f, row.height),
                WorkMonitorUtility.FormatDuration(stats.TotalTicksSpent, WorkMonitorMod.Settings?.showTimeInHours ?? true));
            x += 55f;
            Widgets.Label(new Rect(x, row.y, 55f, row.height), stats.WorkedCount + "/" + stats.EnabledCount);
            x += 60f;
            Widgets.Label(new Rect(x, row.y, 80f, row.height),
                "WorkMonitor.InterestShort".Translate(stats.MajorInterestCount, stats.MinorInterestCount));
        }
    }
}
