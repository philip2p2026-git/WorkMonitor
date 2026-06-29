using System.Collections.Generic;
using UnityEngine;
using Verse;
using WorkMonitor.Groups;

namespace WorkMonitor.UI
{
    public class WorkGroupDetailPanel
    {
        private readonly WorkGroupChartPanel chartPanel = new WorkGroupChartPanel();
        private Vector2 scroll;
        private WorkGroupStats stats;
        private List<WorkGroupStats> allStats = new List<WorkGroupStats>();

        public void SetGroup(WorkGroupSnapshot group)
        {
            allStats = WorkGroupStatsAggregator.BuildAll();
            stats = WorkGroupStatsAggregator.Build(group);
        }

        public void Draw(Rect rect, out bool back, out bool highlight)
        {
            back = false;
            highlight = false;

            if (stats == null)
            {
                return;
            }

            if (Widgets.ButtonText(new Rect(rect.x, rect.y, 80f, 28f), "WorkMonitor.Back".Translate()))
            {
                back = true;
                return;
            }

            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(rect.x + 90f, rect.y, rect.width - 280f, 30f),
                "WorkMonitor.DetailTitle".Translate(stats.Group.Label));

            if (Widgets.ButtonText(new Rect(rect.xMax - 180f, rect.y, 170f, 28f), "WorkMonitor.HighlightInWorkTab".Translate()))
            {
                highlight = true;
            }

            Rect header = new Rect(rect.x, rect.y + 34f, rect.width, 48f);
            DrawHeader(header);

            Rect chartRect = new Rect(rect.x, header.yMax + 6f, rect.width, 150f);
            chartPanel.Draw(chartRect, stats, allStats);

            Rect content = new Rect(rect.x, chartRect.yMax + 8f, rect.width, rect.yMax - chartRect.yMax - 16f);
            Rect view = new Rect(0f, 0f, content.width - 16f, 220f + stats.WorkGiverStats.Count * 24f + stats.ColonistStats.Count * 28f);
            Widgets.BeginScrollView(content, ref scroll, view);

            float y = 0f;
            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(0f, y, view.width, 22f), "WorkMonitor.WorkGivers".Translate());
            y += 24f;
            foreach (WorkGiverStat wg in stats.WorkGiverStats)
            {
                string time = WorkMonitorUtility.FormatDuration(wg.TicksSpent, WorkMonitorMod.Settings?.showTimeInHours ?? true);
                Widgets.Label(new Rect(0f, y, view.width, 22f),
                    "WorkMonitor.WorkGiverRow".Translate(wg.Label, wg.JobCount, time));
                y += 24f;
            }

            y += 8f;
            Widgets.Label(new Rect(0f, y, view.width, 22f), "WorkMonitor.Colonists".Translate());
            y += 24f;
            foreach (ColonistWorkStat colonist in stats.ColonistStats)
            {
                Rect row = new Rect(0f, y, view.width, 24f);
                string passion = WorkMonitorUiUtility.PassionShort(colonist.Passion);
                string time = WorkMonitorUtility.FormatDuration(colonist.TicksSpent, WorkMonitorMod.Settings?.showTimeInHours ?? true);
                Widgets.Label(new Rect(row.x, row.y, 160f, row.height), (passion + " " + colonist.Label).Trim());
                Widgets.Label(new Rect(row.x + 165f, row.y, 180f, row.height), colonist.JobCount + " jobs · " + time);
                Rect bar = new Rect(row.x + 350f, row.y + 8f, row.width - 410f, 10f);
                WorkMonitorUiUtility.DrawShareBar(bar, colonist.PercentOfGroupTime);
                Widgets.Label(new Rect(row.xMax - 48f, row.y, 44f, row.height), colonist.PercentOfGroupTime.ToString("0") + "%");
                y += 28f;
            }

            Widgets.EndScrollView();
        }

        private void DrawHeader(Rect rect)
        {
            Color dot = WorkMonitorUiUtility.StatusColor(stats.Status);
            Widgets.DrawBoxSolid(new Rect(rect.x, rect.y + 6f, 12f, 12f), dot);
            string summary = string.Format(
                "capable {0} · enabled {1} · worked {2} · {3}",
                stats.CapableCount,
                stats.EnabledCount,
                stats.WorkedCount,
                "WorkMonitor.InterestShort".Translate(stats.MajorInterestCount, stats.MinorInterestCount));
            Widgets.Label(new Rect(rect.x + 18f, rect.y, rect.width, 22f), summary);
            string totals = "WorkMonitor.JobsTimeSummary".Translate(
                stats.TotalJobCount,
                WorkMonitorUtility.FormatDuration(stats.TotalTicksSpent, WorkMonitorMod.Settings?.showTimeInHours ?? true));
            Widgets.Label(new Rect(rect.x + 18f, rect.y + 22f, rect.width, 22f), totals);
        }
    }
}
