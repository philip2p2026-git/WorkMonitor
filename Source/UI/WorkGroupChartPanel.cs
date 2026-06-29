using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using WorkMonitor.Groups;
using WorkMonitor.Tracking;

namespace WorkMonitor.UI
{
    public class WorkGroupChartPanel
    {
        public int RangeHours = 24;

        public void Draw(Rect rect, WorkGroupStats stats, List<WorkGroupStats> allStats)
        {
            Text.Font = GameFont.Tiny;
            Widgets.Label(new Rect(rect.x, rect.y, 50f, 18f), "WorkMonitor.Range".Translate());
            if (Widgets.ButtonText(new Rect(rect.x + 52f, rect.y, 110f, 24f), "WorkMonitor.LastHours".Translate(RangeHours)))
            {
                RangeHours = RangeHours switch
                {
                    6 => 12,
                    12 => 24,
                    24 => 48,
                    _ => 6
                };
            }

            WorkActivityTracker tracker = WorkActivityTracker.EnsureRegistered();
            WorkHistoryRingBuffer history = tracker?.GetGroupHistory(stats.Group.Key.StorageKey);
            int minHour = WorkMonitorUtility.CurrentHourIndex() - RangeHours;
            string groupKey = stats.Group.Key.StorageKey;

            float gap = 8f;
            float cellW = (rect.width - gap) / 2f;
            float chartY = rect.y + 28f;
            float chartH = rect.height - 28f;

            WorkChartDataBuilder.BuildJobCountSeries(history, minHour, out float[] colonistJobs, out _);
            WorkChartDataBuilder.BuildMapOpenTasksSeries(history, groupKey, minHour, out float[] mapJobs);
            DualLineChart.Draw(
                new Rect(rect.x, chartY, cellW, chartH),
                colonistJobs,
                mapJobs,
                "WorkMonitor.MetricJobCount".Translate(),
                "WorkMonitor.JobProcessed".Translate(),
                "WorkMonitor.ExistJob".Translate());

            WorkChartDataBuilder.BuildWorkUnitsSeries(history, minHour, out float[] colonistWork, out _);
            WorkChartDataBuilder.BuildMapWorkLeftSeries(history, groupKey, minHour, out float[] mapWork);
            DualLineChart.Draw(
                new Rect(rect.x + cellW + gap, chartY, cellW, chartH),
                colonistWork,
                mapWork,
                "WorkMonitor.MetricWorkUnits".Translate(),
                "WorkMonitor.WorkProcessed".Translate(),
                "WorkMonitor.ExistWork".Translate());
        }
    }
}
