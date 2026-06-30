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
        public WorkChartDisplayMode DisplayMode = WorkChartDisplayMode.Line;

        public void Draw(Rect rect, WorkGroupStats stats, List<WorkGroupStats> allStats, MonitorRangeState rangeState, System.Action onRangeChanged)
        {
            Text.Font = GameFont.Tiny;
            Widgets.Label(new Rect(rect.x, rect.y, 50f, 18f), "WorkMonitor.Range".Translate());
            if (Widgets.ButtonText(new Rect(rect.x + 52f, rect.y, 110f, 24f), "WorkMonitor.LastHours".Translate(rangeState.RangeHours)))
            {
                rangeState.CycleRangeHours();
                onRangeChanged?.Invoke();
            }

            string modeLabel = DisplayMode == WorkChartDisplayMode.Stream
                ? "WorkMonitor.ChartModeStream".Translate()
                : "WorkMonitor.ChartModeLine".Translate();
            if (Widgets.ButtonText(new Rect(rect.x + 168f, rect.y, 90f, 24f), modeLabel))
            {
                DisplayMode = DisplayMode == WorkChartDisplayMode.Line
                    ? WorkChartDisplayMode.Stream
                    : WorkChartDisplayMode.Line;
            }

            WorkActivityTracker tracker = WorkActivityTracker.EnsureRegistered();
            WorkHistoryRingBuffer history = tracker?.GetGroupHistory(stats.Group.Key.StorageKey);
            int minHour = rangeState.MinHourIndex;
            int rangeHours = rangeState.RangeHours;
            string groupKey = stats.Group.Key.StorageKey;

            float gap = 8f;
            float cellW = (rect.width - gap) / 2f;
            float chartY = rect.y + 28f;
            float chartH = rect.height - 28f;

            WorkChartDataBuilder.BuildJobCountSeries(history, minHour, rangeHours, out float[] colonistJobs, out string[] jobLabels);
            WorkChartDataBuilder.BuildMapOpenTasksSeries(history, groupKey, minHour, rangeHours, out float[] mapJobs, out _);
            DrawChart(
                new Rect(rect.x, chartY, cellW, chartH),
                colonistJobs,
                mapJobs,
                jobLabels,
                "WorkMonitor.MetricJobCount".Translate(),
                "WorkMonitor.JobProcessed".Translate(),
                "WorkMonitor.ExistJob".Translate());

            WorkChartDataBuilder.BuildWorkUnitsSeries(history, minHour, rangeHours, out float[] colonistWork, out string[] workLabels);
            WorkChartDataBuilder.BuildMapWorkLeftSeries(history, groupKey, minHour, rangeHours, out float[] mapWork, out _);
            DrawChart(
                new Rect(rect.x + cellW + gap, chartY, cellW, chartH),
                colonistWork,
                mapWork,
                workLabels,
                "WorkMonitor.MetricWorkUnits".Translate(),
                "WorkMonitor.WorkProcessed".Translate(),
                "WorkMonitor.ExistWork".Translate());
        }

        private void DrawChart(
            Rect rect,
            float[] colonistValues,
            float[] mapValues,
            string[] xLabels,
            string title,
            string colonistLegend,
            string mapLegend)
        {
            if (DisplayMode == WorkChartDisplayMode.Stream)
            {
                DualStreamChart.Draw(rect, colonistValues, mapValues, xLabels, title, colonistLegend, mapLegend);
            }
            else
            {
                DualLineChart.Draw(rect, colonistValues, mapValues, xLabels, title, colonistLegend, mapLegend);
            }
        }
    }
}
