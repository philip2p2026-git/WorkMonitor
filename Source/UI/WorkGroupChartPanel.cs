using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using WorkMonitor.Groups;
using WorkMonitor.Tracking;

namespace WorkMonitor.UI
{
    public class WorkGroupChartPanel
    {
        public WorkChartMetric Metric = WorkChartMetric.TimeConsumed;
        public int RangeHours = 24;

        public void Draw(Rect rect, WorkGroupStats stats, List<WorkGroupStats> allStats)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(rect);

            if (listing.ButtonTextLabeled("WorkMonitor.Metric".Translate(), MetricLabel(Metric)))
            {
                Metric = (WorkChartMetric)(((int)Metric + 1) % 4);
            }

            if (listing.ButtonTextLabeled("WorkMonitor.Range".Translate(), "WorkMonitor.LastHours".Translate(RangeHours)))
            {
                RangeHours = RangeHours switch
                {
                    6 => 12,
                    12 => 24,
                    24 => 48,
                    _ => 6
                };
            }

            listing.Gap(6f);
            listing.End();

            Rect chartRect = new Rect(rect.x, rect.y + 56f, rect.width, rect.height - 56f);
            WorkActivityTracker tracker = WorkActivityTracker.EnsureRegistered();
            WorkHistoryRingBuffer history = tracker?.GetGroupHistory(stats.Group.Key.StorageKey);
            int minHour = WorkMonitorUtility.CurrentHourIndex() - RangeHours;

            switch (Metric)
            {
                case WorkChartMetric.JobCount:
                    WorkChartDataBuilder.BuildJobCountSeries(history, minHour, out float[] jobs, out _);
                    SimpleLineChart.Draw(chartRect, jobs, "WorkMonitor.MetricJobCount".Translate());
                    break;
                case WorkChartMetric.TimeConsumed:
                    WorkChartDataBuilder.BuildLineSeries(history, minHour, out float[] time, out _);
                    SimpleLineChart.Draw(chartRect, time, "WorkMonitor.MetricTimeConsumed".Translate());
                    break;
                case WorkChartMetric.WorkAmong:
                    StackedAreaChart.Draw(chartRect, history, minHour, WorkMonitorUtility.MonitorColonists().ToList());
                    break;
                case WorkChartMetric.RelativeShare:
                    DrawRelativeShare(chartRect, stats, allStats, minHour);
                    break;
            }
        }

        private static void DrawRelativeShare(Rect rect, WorkGroupStats stats, List<WorkGroupStats> allStats, int minHour)
        {
            WorkActivityTracker tracker = WorkActivityTracker.EnsureRegistered();
            WorkHistoryRingBuffer groupHistory = tracker?.GetGroupHistory(stats.Group.Key.StorageKey);
            if (groupHistory == null)
            {
                SimpleLineChart.Draw(rect, new float[0], "WorkMonitor.MetricRelativeShare".Translate());
                return;
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

            SimpleLineChart.Draw(rect, values.ToArray(), "WorkMonitor.MetricRelativeShare".Translate());
        }

        private static string MetricLabel(WorkChartMetric metric)
        {
            return metric switch
            {
                WorkChartMetric.JobCount => "WorkMonitor.MetricJobCount".Translate(),
                WorkChartMetric.TimeConsumed => "WorkMonitor.MetricTimeConsumed".Translate(),
                WorkChartMetric.WorkAmong => "WorkMonitor.MetricWorkAmong".Translate(),
                _ => "WorkMonitor.MetricRelativeShare".Translate()
            };
        }
    }
}
