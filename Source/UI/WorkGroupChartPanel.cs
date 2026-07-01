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
        public void Draw(Rect rect, WorkGroupStats stats, List<WorkGroupStats> allStats, MonitorRangeState rangeState)
        {
            WorkActivityTracker tracker = WorkActivityTracker.EnsureRegistered();
            WorkHistoryTierBuffer history = tracker?.GetGroupHistory(stats.Group.Key.StorageKey);
            int minHour = rangeState.MinHourIndex;
            int rangeHours = rangeState.RangeHours;
            string groupKey = stats.Group.Key.StorageKey;

            float gap = 8f;
            float cellW = (rect.width - gap) / 2f;
            float chartY = rect.y;
            float chartH = rect.height;

            WorkChartDataBuilder.BuildJobCountSeries(history, minHour, rangeHours, rangeState.UsesHourlyChart, out float[] colonistJobs, out string[] jobLabels);
            WorkChartDataBuilder.BuildMapOpenTasksSeries(history, groupKey, minHour, rangeHours, out float[] mapJobs, out float[] mapNewJobs, out _);
            DrawChart(
                new Rect(rect.x, chartY, cellW, chartH),
                colonistJobs,
                mapJobs,
                mapNewJobs,
                jobLabels,
                "WorkMonitor.MetricJobCount".Translate(),
                "WorkMonitor.JobProcessed".Translate(),
                "WorkMonitor.ExistJob".Translate(),
                "WorkMonitor.ChartExistJob".Translate(),
                "WorkMonitor.ChartNewJobToday".Translate());

            WorkChartDataBuilder.BuildWorkUnitsSeries(history, minHour, rangeHours, rangeState.UsesHourlyChart, out float[] colonistWork, out string[] workLabels);
            WorkChartDataBuilder.BuildMapWorkLeftSeries(history, groupKey, minHour, rangeHours, out float[] mapWork, out float[] mapNewWork, out _);
            DrawChart(
                new Rect(rect.x + cellW + gap, chartY, cellW, chartH),
                colonistWork,
                mapWork,
                mapNewWork,
                workLabels,
                "WorkMonitor.MetricWorkUnits".Translate(),
                "WorkMonitor.WorkProcessed".Translate(),
                "WorkMonitor.ExistWork".Translate(),
                "WorkMonitor.ChartExistWork".Translate(),
                "WorkMonitor.ChartNewWorkToday".Translate());
        }

        private void DrawChart(
            Rect rect,
            float[] colonistValues,
            float[] mapValues,
            float[] mapNewTodayValues,
            string[] xLabels,
            string title,
            string colonistLegend,
            string mapLegend,
            string mapExistingLegend,
            string mapNewTodayLegend)
        {
            DualStreamChart.Draw(rect, colonistValues, mapValues, mapNewTodayValues, xLabels, title, colonistLegend, mapLegend, mapExistingLegend, mapNewTodayLegend);
        }
    }
}
