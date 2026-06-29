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

            float gap = 6f;
            float cellW = (rect.width - gap * 2f) / 3f;
            float chartY = rect.y + 28f;
            float chartH = rect.height - 28f;

            WorkChartDataBuilder.BuildJobCountSeries(history, minHour, out float[] jobs, out _);
            WorkChartDataBuilder.BuildWorkUnitsSeries(history, minHour, out float[] workUnits, out _);
            float[] share = BuildRelativeShareSeries(stats, allStats, minHour);

            SimpleLineChart.Draw(
                new Rect(rect.x, chartY, cellW, chartH),
                jobs,
                "WorkMonitor.MetricJobCount".Translate());
            SimpleLineChart.Draw(
                new Rect(rect.x + cellW + gap, chartY, cellW, chartH),
                workUnits,
                "WorkMonitor.MetricWorkUnits".Translate());
            SimpleLineChart.Draw(
                new Rect(rect.x + (cellW + gap) * 2f, chartY, cellW, chartH),
                share,
                "WorkMonitor.MetricRelativeShare".Translate());
        }

        private static float[] BuildRelativeShareSeries(WorkGroupStats stats, List<WorkGroupStats> allStats, int minHour)
        {
            WorkActivityTracker tracker = WorkActivityTracker.EnsureRegistered();
            WorkHistoryRingBuffer groupHistory = tracker?.GetGroupHistory(stats.Group.Key.StorageKey);
            if (groupHistory == null)
            {
                return new float[0];
            }

            List<float> values = new List<float>();
            foreach (var bucket in groupHistory.Buckets)
            {
                if (bucket.hourIndex < minHour)
                {
                    continue;
                }

                int colonyTicks = 0;
                foreach (WorkGroupStats other in allStats)
                {
                    WorkHistoryRingBuffer otherHistory = tracker.GetGroupHistory(other.Group.Key.StorageKey);
                    foreach (var otherBucket in otherHistory.Buckets)
                    {
                        if (otherBucket.hourIndex == bucket.hourIndex)
                        {
                            colonyTicks += otherBucket.ticksSpent;
                        }
                    }
                }

                values.Add(colonyTicks > 0 ? bucket.ticksSpent / (float)colonyTicks * 100f : 0f);
            }

            return values.ToArray();
        }
    }
}
