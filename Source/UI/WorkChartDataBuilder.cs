using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using WorkMonitor.Groups;
using WorkMonitor.Tracking;

namespace WorkMonitor.UI
{
    public enum WorkChartDisplayMode
    {
        Line,
        Stream
    }

    public enum WorkChartMetric
    {
        JobCount,
        WorkUnits,
        WorkAmong,
        RelativeShare
    }

    public static class WorkChartDataBuilder
    {
        public static void BuildWorkUnitsSeries(
            WorkHistoryTierBuffer history,
            int minHourIndex,
            int rangeHours,
            bool useHourly,
            out float[] values,
            out string[] labels)
        {
            if (useHourly)
            {
                BuildFixedHourlySeries(
                    history,
                    minHourIndex,
                    rangeHours,
                    bucket => bucket != null ? bucket.workUnitsSpent + bucket.estimatedWorkUnitsSpent : 0f,
                    out values,
                    out labels);
            }
            else
            {
                BuildDailySeries(
                    history,
                    minHourIndex,
                    rangeHours,
                    d => d.workUnitsSpent + d.estimatedWorkUnitsSpent,
                    b => b.workUnitsSpent + b.estimatedWorkUnitsSpent,
                    out values,
                    out labels);
            }
        }

        public static void BuildJobCountSeries(
            WorkHistoryTierBuffer history,
            int minHourIndex,
            int rangeHours,
            bool useHourly,
            out float[] values,
            out string[] labels)
        {
            if (useHourly)
            {
                BuildFixedHourlySeries(
                    history,
                    minHourIndex,
                    rangeHours,
                    bucket => bucket != null ? bucket.jobCount : 0f,
                    out values,
                    out labels);
            }
            else
            {
                BuildDailySeries(
                    history,
                    minHourIndex,
                    rangeHours,
                    d => d.jobCount,
                    b => b.jobCount,
                    out values,
                    out labels);
            }
        }

        public static void BuildMapOpenTasksSeries(
            WorkHistoryTierBuffer colonistHistory,
            string groupStorageKey,
            int minHourIndex,
            int rangeHours,
            out float[] values,
            out float[] newTodayValues,
            out string[] labels)
        {
            BuildMapMetricSeries(colonistHistory, minHourIndex, rangeHours, groupStorageKey, jobs: true, out values, out newTodayValues, out labels);
        }

        public static void BuildMapWorkLeftSeries(
            WorkHistoryTierBuffer colonistHistory,
            string groupStorageKey,
            int minHourIndex,
            int rangeHours,
            out float[] values,
            out float[] newTodayValues,
            out string[] labels)
        {
            BuildMapMetricSeries(colonistHistory, minHourIndex, rangeHours, groupStorageKey, jobs: false, out values, out newTodayValues, out labels);
        }

        private static void BuildFixedHourlySeries(
            WorkHistoryTierBuffer history,
            int minHourIndex,
            int rangeHours,
            System.Func<HourlyWorkBucket, float> hourlySelector,
            out float[] values,
            out string[] labels)
        {
            values = new float[rangeHours];
            labels = new string[rangeHours];
            for (int i = 0; i < rangeHours; i++)
            {
                int hour = minHourIndex + i;
                HourlyWorkBucket bucket = history?.GetBucket(hour);
                values[i] = bucket != null ? hourlySelector(bucket) : 0f;
                labels[i] = BuildRelativeHourLabel(i, rangeHours);
            }
        }

        private static string BuildRelativeHourLabel(int index, int rangeHours)
        {
            if (index == rangeHours - 1)
            {
                return "now";
            }

            int hoursAgo = rangeHours - 1 - index;
            return "-" + hoursAgo + "h";
        }

        private static void BuildDailySeries(
            WorkHistoryTierBuffer history,
            int minHourIndex,
            int rangeHours,
            System.Func<DailyWorkBucket, float> dailySelector,
            System.Func<HourlyWorkBucket, float> hourlySelector,
            out float[] values,
            out string[] labels)
        {
            int dayCount = Mathf.Clamp(rangeHours / 24, 1, 14);
            values = new float[dayCount];
            labels = new string[dayCount];
            int currentDayId = WorkMonitorUtility.CurrentWorkDayId();

            for (int i = 0; i < dayCount; i++)
            {
                int targetDayId = currentDayId - (dayCount - 1 - i);
                float hourlySum = 0f;
                if (history != null)
                {
                    foreach (HourlyWorkBucket bucket in history.Buckets)
                    {
                        if (WorkMonitorUtility.GetWorkDayIdForHourIndex(bucket.hourIndex) == targetDayId)
                        {
                            hourlySum += hourlySelector(bucket);
                        }
                    }
                }

                DailyWorkBucket dailyBucket = history?.DailyBuckets?.FirstOrDefault(d => d.dayId == targetDayId);
                float dailyTotal = dailyBucket != null ? dailySelector(dailyBucket) : 0f;
                values[i] = Mathf.Max(hourlySum, dailyTotal);
                labels[i] = i == dayCount - 1 ? "now" : "-" + (dayCount - 1 - i) + "d";
            }
        }

        private static void BuildMapMetricSeries(
            WorkHistoryTierBuffer colonistHistory,
            int minHourIndex,
            int rangeHours,
            string groupStorageKey,
            bool jobs,
            out float[] values,
            out float[] newTodayValues,
            out string[] labels)
        {
            values = new float[rangeHours];
            newTodayValues = new float[rangeHours];
            labels = new string[rangeHours];

            MapWorkSampler sampler = MapWorkSampler.EnsureRegistered();
            IReadOnlyList<MapWorkSnapshot> mapHistory = sampler?.GetHistory();
            SeedMapSeries(
                mapHistory,
                minHourIndex,
                groupStorageKey,
                jobs: jobs,
                workGiverDefName: null,
                out float lastValue,
                out float lastNewToday,
                out int mapIndex);

            for (int i = 0; i < rangeHours; i++)
            {
                int hour = minHourIndex + i;
                labels[i] = BuildRelativeHourLabel(i, rangeHours);

                if (mapHistory != null)
                {
                    while (mapIndex < mapHistory.Count && mapHistory[mapIndex].hourIndex <= hour)
                    {
                        TryReadMapGroupSnapshot(mapHistory[mapIndex], groupStorageKey, jobs, ref lastValue, ref lastNewToday);
                        mapIndex++;
                    }
                }

                values[i] = lastValue;
                newTodayValues[i] = lastNewToday;
            }
        }

        private static void SeedMapSeries(
            IReadOnlyList<MapWorkSnapshot> mapHistory,
            int minHourIndex,
            string groupStorageKey,
            bool jobs,
            string workGiverDefName,
            out float lastValue,
            out float lastNewToday,
            out int startIndex)
        {
            lastValue = 0f;
            lastNewToday = 0f;
            startIndex = 0;
            if (mapHistory == null)
            {
                return;
            }

            for (int i = 0; i < mapHistory.Count; i++)
            {
                MapWorkSnapshot snap = mapHistory[i];
                if (snap.hourIndex > minHourIndex)
                {
                    if (i == 0)
                    {
                        if (!workGiverDefName.NullOrEmpty())
                        {
                            TryReadMapWorkGiverSnapshot(snap, workGiverDefName, jobs, ref lastValue, ref lastNewToday);
                        }
                        else
                        {
                            TryReadMapGroupSnapshot(snap, groupStorageKey, jobs, ref lastValue, ref lastNewToday);
                        }
                    }

                    startIndex = i;
                    return;
                }

                if (!workGiverDefName.NullOrEmpty())
                {
                    TryReadMapWorkGiverSnapshot(snap, workGiverDefName, jobs, ref lastValue, ref lastNewToday);
                }
                else
                {
                    TryReadMapGroupSnapshot(snap, groupStorageKey, jobs, ref lastValue, ref lastNewToday);
                }

                startIndex = i + 1;
            }

            startIndex = mapHistory.Count;
        }

        private static void TryReadMapGroupSnapshot(
            MapWorkSnapshot snap,
            string groupStorageKey,
            bool jobs,
            ref float lastValue,
            ref float lastNewToday)
        {
            if (snap.perGroupKey.TryGetValue(groupStorageKey, out MapWorkGroupSnapshot groupSnap))
            {
                lastValue = jobs ? groupSnap.openTaskCount : groupSnap.workLeftTotal;
                lastNewToday = jobs ? groupSnap.newTodayOpenTaskCount : groupSnap.newTodayWorkLeftTotal;
            }
        }

        private static void TryReadMapWorkGiverSnapshot(
            MapWorkSnapshot snap,
            string workGiverDefName,
            bool jobs,
            ref float lastValue,
            ref float lastNewToday)
        {
            if (snap.perWorkGiver.TryGetValue(workGiverDefName, out MapWorkGiverSnapshot wgSnap))
            {
                lastValue = jobs ? wgSnap.openTaskCount : wgSnap.workLeftTotal;
                lastNewToday = jobs ? wgSnap.newTodayOpenTaskCount : wgSnap.newTodayWorkLeftTotal;
            }
        }

        public static void BuildMapOpenTasksSeriesForWorkGiver(
            string workGiverDefName,
            int minHourIndex,
            int rangeHours,
            out float[] values,
            out float[] newTodayValues,
            out string[] labels)
        {
            BuildMapMetricSeriesForWorkGiver(workGiverDefName, minHourIndex, rangeHours, jobs: true, out values, out newTodayValues, out labels);
        }

        public static void BuildMapWorkLeftSeriesForWorkGiver(
            string workGiverDefName,
            int minHourIndex,
            int rangeHours,
            out float[] values,
            out float[] newTodayValues,
            out string[] labels)
        {
            BuildMapMetricSeriesForWorkGiver(workGiverDefName, minHourIndex, rangeHours, jobs: false, out values, out newTodayValues, out labels);
        }

        private static void BuildMapMetricSeriesForWorkGiver(
            string workGiverDefName,
            int minHourIndex,
            int rangeHours,
            bool jobs,
            out float[] values,
            out float[] newTodayValues,
            out string[] labels)
        {
            values = new float[rangeHours];
            newTodayValues = new float[rangeHours];
            labels = new string[rangeHours];

            MapWorkSampler sampler = MapWorkSampler.EnsureRegistered();
            IReadOnlyList<MapWorkSnapshot> mapHistory = sampler?.GetHistory();
            SeedMapSeries(
                mapHistory,
                minHourIndex,
                groupStorageKey: null,
                jobs: jobs,
                workGiverDefName: workGiverDefName,
                out float lastValue,
                out float lastNewToday,
                out int mapIndex);

            for (int i = 0; i < rangeHours; i++)
            {
                int hour = minHourIndex + i;
                labels[i] = BuildRelativeHourLabel(i, rangeHours);

                if (mapHistory != null)
                {
                    while (mapIndex < mapHistory.Count && mapHistory[mapIndex].hourIndex <= hour)
                    {
                        TryReadMapWorkGiverSnapshot(mapHistory[mapIndex], workGiverDefName, jobs, ref lastValue, ref lastNewToday);
                        mapIndex++;
                    }
                }

                values[i] = lastValue;
                newTodayValues[i] = lastNewToday;
            }
        }
    }

    public static class ChartAxisHelper
    {
        public static void DrawXAxisLabels(Rect plot, string[] labels)
        {
            if (labels == null || labels.Length == 0)
            {
                return;
            }

            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.UpperCenter;
            Color prev = GUI.color;
            GUI.color = new Color(0.7f, 0.7f, 0.7f);

            int step = labels.Length > 24 ? 2 : 1;
            float colWidth = plot.width / Mathf.Max(1, labels.Length);
            Rect labelRow = new Rect(plot.x, plot.yMax + 2f, colWidth, 14f);
            for (int i = 0; i < labels.Length; i++)
            {
                if (i % step != 0 && i != labels.Length - 1)
                {
                    continue;
                }

                Rect cell = new Rect(plot.x + i * colWidth, labelRow.y, colWidth, labelRow.height);
                Widgets.Label(cell, labels[i]);
            }

            GUI.color = prev;
            Text.Anchor = TextAnchor.UpperLeft;
        }
    }

    public static class DualLineChart
    {
        private static readonly Color ColonistColor = new Color(0.4f, 0.85f, 0.5f);
        private static readonly Color MapColor = new Color(0.95f, 0.65f, 0.3f);
        private static readonly Color MapExistingColor = new Color(1f, 0.65f, 0.2f, 0.45f);
        private static readonly Color MapNewTodayColor = new Color(1f, 0.45f, 0f, 0.85f);
        private static readonly Color GridColor = new Color(0.45f, 0.45f, 0.45f, 0.35f);

        public static void Draw(
            Rect rect,
            float[] colonistValues,
            float[] mapValues,
            string[] xLabels,
            string title,
            string colonistLegend,
            string mapLegend)
        {
            Draw(rect, colonistValues, mapValues, null, xLabels, title, colonistLegend, mapLegend, null, null);
        }

        public static void Draw(
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
            Widgets.DrawBoxSolid(rect, new Color(0.1f, 0.1f, 0.1f, 0.35f));
            Text.Font = GameFont.Tiny;
            Widgets.Label(new Rect(rect.x + 4f, rect.y + 2f, rect.width - 8f, 16f), title);

            float legendY = rect.y + 18f;
            DrawLegendEntry(new Rect(rect.x + 6f, legendY, rect.width * 0.5f - 8f, 14f), ColonistColor, colonistLegend);
            if (mapNewTodayValues != null && !mapExistingLegend.NullOrEmpty())
            {
                DrawLegendEntry(new Rect(rect.x + rect.width * 0.5f, legendY, rect.width * 0.25f - 4f, 14f), MapExistingColor, mapExistingLegend);
                DrawLegendEntry(new Rect(rect.x + rect.width * 0.75f, legendY, rect.width * 0.25f - 4f, 14f), MapNewTodayColor, mapNewTodayLegend);
            }
            else
            {
                DrawLegendEntry(new Rect(rect.x + rect.width * 0.5f, legendY, rect.width * 0.5f - 6f, 14f), MapColor, mapLegend);
            }

            const float axisWidth = 30f;
            const float xAxisHeight = 16f;
            Rect plot = new Rect(rect.x + axisWidth + 4f, rect.y + 34f, rect.width - axisWidth - 12f, rect.height - 40f - xAxisHeight);
            if (colonistValues == null || colonistValues.Length == 0)
            {
                Widgets.Label(plot, "-");
                return;
            }

            float colonistYMax = ComputeYMax(colonistValues, null, stacked: false);
            DrawYAxis(new Rect(rect.x + 2f, plot.y, axisWidth, plot.height), colonistYMax);
            DrawGrid(plot, colonistYMax);

            const float mapBandFraction = 0.32f;
            bool hasMap = mapValues != null && mapValues.Length == colonistValues.Length;
            Rect colonistPlot = hasMap
                ? new Rect(plot.x, plot.y, plot.width, plot.height * (1f - mapBandFraction))
                : plot;
            Rect mapPlot = hasMap
                ? new Rect(plot.x, plot.yMax - plot.height * mapBandFraction, plot.width, plot.height * mapBandFraction)
                : Rect.zero;

            DrawSeries(colonistPlot, colonistValues, colonistYMax, ColonistColor);
            if (hasMap)
            {
                float mapYMax = ComputeYMax(mapValues, null, stacked: false);
                Widgets.DrawLineHorizontal(mapPlot.x, mapPlot.y, mapPlot.width, new Color(1f, 1f, 1f, 0.15f));
                if (mapNewTodayValues != null && mapNewTodayValues.Length == mapValues.Length)
                {
                    DrawStackedMapFill(mapPlot, mapValues, mapNewTodayValues, mapYMax);
                }
                else
                {
                    DrawSeries(mapPlot, mapValues, mapYMax, MapColor);
                }
            }

            ChartAxisHelper.DrawXAxisLabels(plot, xLabels);
        }

        private static void DrawStackedMapFill(Rect plot, float[] mapTotals, float[] mapNewToday, float yMax)
        {
            int count = mapTotals.Length;
            float colWidth = plot.width / Mathf.Max(1, count - 1);
            for (int i = 0; i < count; i++)
            {
                float x = plot.x + plot.width * i / Mathf.Max(1, count - 1);
                float existing = mapTotals[i] - mapNewToday[i];
                float existingHeight = plot.height * (existing / yMax);
                float newHeight = plot.height * (mapNewToday[i] / yMax);
                float sliceWidth = Mathf.Max(2f, colWidth * 0.6f);

                if (existingHeight > 0f)
                {
                    Widgets.DrawBoxSolid(new Rect(x - sliceWidth * 0.5f, plot.yMax - existingHeight, sliceWidth, existingHeight), MapExistingColor);
                }

                if (newHeight > 0f)
                {
                    Widgets.DrawBoxSolid(new Rect(x - sliceWidth * 0.5f, plot.yMax - existingHeight - newHeight, sliceWidth, newHeight), MapNewTodayColor);
                }
            }
        }

        private static float ComputeYMax(float[] colonistValues, float[] mapValues, bool stacked)
        {
            float yMax = 0.01f;
            for (int i = 0; i < colonistValues.Length; i++)
            {
                float value = colonistValues[i];
                if (stacked && mapValues != null && i < mapValues.Length)
                {
                    value += mapValues[i];
                }

                if (value > yMax)
                {
                    yMax = value;
                }
            }

            if (!stacked && mapValues != null)
            {
                foreach (float v in mapValues)
                {
                    if (v > yMax)
                    {
                        yMax = v;
                    }
                }
            }

            return yMax * 1.2f;
        }

        private static void DrawGrid(Rect plot, float yMax)
        {
            for (int tick = 1; tick <= 3; tick++)
            {
                float fraction = tick / 4f;
                float y = plot.yMax - plot.height * fraction;
                Widgets.DrawLine(new Vector2(plot.x, y), new Vector2(plot.xMax, y), GridColor, 1f);
            }
        }

        private static void DrawLegendEntry(Rect rect, Color color, string label)
        {
            Widgets.DrawBoxSolid(new Rect(rect.x, rect.y + 3f, 10f, 10f), color);
            Color prev = GUI.color;
            GUI.color = new Color(0.75f, 0.75f, 0.75f);
            Widgets.Label(new Rect(rect.x + 14f, rect.y, rect.width - 14f, rect.height), label);
            GUI.color = prev;
        }

        private static void DrawSeries(Rect plot, float[] values, float yMax, Color color)
        {
            Vector2 prev = default;
            for (int i = 0; i < values.Length; i++)
            {
                float x = plot.x + plot.width * i / Mathf.Max(1, values.Length - 1);
                float y = plot.yMax - plot.height * (values[i] / yMax);
                if (i > 0)
                {
                    Widgets.DrawLine(prev, new Vector2(x, y), color, 1f);
                }

                prev = new Vector2(x, y);
            }
        }

        private static void DrawYAxis(Rect axis, float yMax)
        {
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleRight;
            Color prev = GUI.color;
            GUI.color = new Color(0.7f, 0.7f, 0.7f);

            for (int tick = 1; tick <= 3; tick++)
            {
                float fraction = tick / 4f;
                float y = axis.yMax - axis.height * fraction;
                float value = yMax * fraction;
                Widgets.Label(new Rect(axis.x, y - 7f, axis.width - 2f, 14f), FormatAxisValue(value));
            }

            Widgets.Label(new Rect(axis.x, axis.y - 2f, axis.width - 2f, 14f), FormatAxisValue(yMax));

            GUI.color = prev;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private static string FormatAxisValue(float value)
        {
            if (value >= 10000f)
            {
                return (value / 1000f).ToString("0.#") + "k";
            }

            if (value >= 100f)
            {
                return value.ToString("0");
            }

            return value.ToString("0.#");
        }
    }

    public static class DualStreamChart
    {
        private static readonly Color ColonistColor = new Color(0.4f, 0.85f, 0.5f, 0.9f);
        private static readonly Color MapColor = new Color(0.95f, 0.65f, 0.3f, 0.9f);
        private static readonly Color MapExistingColor = new Color(1f, 0.65f, 0.2f, 0.45f);
        private static readonly Color MapNewTodayColor = new Color(1f, 0.45f, 0f, 0.85f);
        private static readonly Color GridColor = new Color(0.45f, 0.45f, 0.45f, 0.35f);

        public static void Draw(
            Rect rect,
            float[] colonistValues,
            float[] mapValues,
            string[] xLabels,
            string title,
            string colonistLegend,
            string mapLegend)
        {
            Draw(rect, colonistValues, mapValues, null, xLabels, title, colonistLegend, mapLegend, null, null);
        }

        public static void Draw(
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
            Widgets.DrawBoxSolid(rect, new Color(0.1f, 0.1f, 0.1f, 0.35f));
            Text.Font = GameFont.Tiny;
            Widgets.Label(new Rect(rect.x + 4f, rect.y + 2f, rect.width - 8f, 16f), title);

            float legendY = rect.y + 18f;
            DrawLegendEntry(new Rect(rect.x + 6f, legendY, rect.width * 0.5f - 8f, 14f), ColonistColor, colonistLegend);
            if (mapNewTodayValues != null && !mapExistingLegend.NullOrEmpty())
            {
                DrawLegendEntry(new Rect(rect.x + rect.width * 0.5f, legendY, rect.width * 0.25f - 4f, 14f), MapExistingColor, mapExistingLegend);
                DrawLegendEntry(new Rect(rect.x + rect.width * 0.75f, legendY, rect.width * 0.25f - 4f, 14f), MapNewTodayColor, mapNewTodayLegend);
            }
            else
            {
                DrawLegendEntry(new Rect(rect.x + rect.width * 0.5f, legendY, rect.width * 0.5f - 6f, 14f), MapColor, mapLegend);
            }

            const float axisWidth = 30f;
            const float xAxisHeight = 16f;
            Rect plot = new Rect(rect.x + axisWidth + 4f, rect.y + 34f, rect.width - axisWidth - 12f, rect.height - 40f - xAxisHeight);
            if (colonistValues == null || colonistValues.Length == 0)
            {
                Widgets.Label(plot, "-");
                return;
            }

            float yMax = ComputeYMax(colonistValues, mapValues);
            DrawYAxis(new Rect(rect.x + 2f, plot.y, axisWidth, plot.height), yMax);

            for (int tick = 1; tick <= 3; tick++)
            {
                float fraction = tick / 4f;
                float y = plot.yMax - plot.height * fraction;
                Widgets.DrawLine(new Vector2(plot.x, y), new Vector2(plot.xMax, y), GridColor, 1f);
            }

            const float barGap = 1f;
            int count = colonistValues.Length;
            float colWidth = plot.width / Mathf.Max(1, count);
            float barWidth = Mathf.Max(1f, (colWidth - barGap) * 0.5f);
            for (int i = 0; i < count; i++)
            {
                float mapValue = mapValues != null && i < mapValues.Length ? mapValues[i] : 0f;
                float mapNewToday = mapNewTodayValues != null && i < mapNewTodayValues.Length ? mapNewTodayValues[i] : 0f;
                float mapExisting = mapValue - mapNewToday;
                float colonistValue = colonistValues[i];
                float slotX = plot.x + i * colWidth;
                float orangeX = slotX;
                float greenX = slotX + barWidth + barGap;
                float yBottom = plot.yMax;

                if (mapValue > 0f)
                {
                    if (mapNewTodayValues != null)
                    {
                        float mapExistingHeight = plot.height * (mapExisting / yMax);
                        float mapNewHeight = plot.height * (mapNewToday / yMax);

                        if (mapExistingHeight > 0f)
                        {
                            Widgets.DrawBoxSolid(new Rect(orangeX, yBottom - mapExistingHeight, barWidth, mapExistingHeight), MapExistingColor);
                            yBottom -= mapExistingHeight;
                        }

                        if (mapNewHeight > 0f)
                        {
                            Widgets.DrawBoxSolid(new Rect(orangeX, yBottom - mapNewHeight, barWidth, mapNewHeight), MapNewTodayColor);
                        }
                    }
                    else
                    {
                        float mapHeight = plot.height * (mapValue / yMax);
                        Widgets.DrawBoxSolid(new Rect(orangeX, plot.yMax - mapHeight, barWidth, mapHeight), MapColor);
                    }

                    if (colonistValue > 0f)
                    {
                        float orangeHeight = plot.height * (mapValue / yMax);
                        float greenClamped = Mathf.Min(colonistValue, mapValue);
                        float greenHeight = plot.height * (greenClamped / yMax);
                        float orangeTopY = plot.yMax - orangeHeight;
                        Widgets.DrawBoxSolid(new Rect(greenX, orangeTopY, barWidth, greenHeight), ColonistColor);
                    }
                }
            }

            ChartAxisHelper.DrawXAxisLabels(plot, xLabels);
        }

        private static float ComputeYMax(float[] colonistValues, float[] mapValues)
        {
            float yMax = 0.01f;
            for (int i = 0; i < colonistValues.Length; i++)
            {
                float mapValue = mapValues != null && i < mapValues.Length ? mapValues[i] : 0f;
                float peak = Mathf.Max(mapValue, colonistValues[i]);
                if (peak > yMax)
                {
                    yMax = peak;
                }
            }

            return yMax * 1.2f;
        }

        private static void DrawLegendEntry(Rect rect, Color color, string label)
        {
            Widgets.DrawBoxSolid(new Rect(rect.x, rect.y + 3f, 10f, 10f), color);
            Color prev = GUI.color;
            GUI.color = new Color(0.75f, 0.75f, 0.75f);
            Widgets.Label(new Rect(rect.x + 14f, rect.y, rect.width - 14f, rect.height), label);
            GUI.color = prev;
        }

        private static void DrawYAxis(Rect axis, float yMax)
        {
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleRight;
            Color prev = GUI.color;
            GUI.color = new Color(0.7f, 0.7f, 0.7f);

            for (int tick = 1; tick <= 3; tick++)
            {
                float fraction = tick / 4f;
                float y = axis.yMax - axis.height * fraction;
                float value = yMax * fraction;
                Widgets.Label(new Rect(axis.x, y - 7f, axis.width - 2f, 14f), FormatAxisValue(value));
            }

            Widgets.Label(new Rect(axis.x, axis.y - 2f, axis.width - 2f, 14f), FormatAxisValue(yMax));

            GUI.color = prev;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private static string FormatAxisValue(float value)
        {
            if (value >= 10000f)
            {
                return (value / 1000f).ToString("0.#") + "k";
            }

            if (value >= 100f)
            {
                return value.ToString("0");
            }

            return value.ToString("0.#");
        }
    }

    public static class SimpleLineChart
    {
        private static readonly Color LineColor = new Color(0.4f, 0.85f, 0.5f);
        private static readonly Color GridColor = new Color(0.45f, 0.45f, 0.45f, 0.35f);

        public static void Draw(Rect rect, float[] values, string title)
        {
            Widgets.DrawBoxSolid(rect, new Color(0.1f, 0.1f, 0.1f, 0.35f));
            Text.Font = GameFont.Tiny;
            Widgets.Label(new Rect(rect.x + 4f, rect.y + 2f, rect.width, 16f), title);

            const float axisWidth = 30f;
            Rect plot = new Rect(rect.x + axisWidth + 4f, rect.y + 20f, rect.width - axisWidth - 12f, rect.height - 26f);
            if (values == null || values.Length == 0)
            {
                Widgets.Label(plot, "-");
                return;
            }

            float dataMax = 0.01f;
            foreach (float v in values)
            {
                if (v > dataMax)
                {
                    dataMax = v;
                }
            }

            float yMax = dataMax * 1.2f;
            DrawYAxis(new Rect(rect.x + 2f, plot.y, axisWidth, plot.height), yMax);

            for (int tick = 1; tick <= 3; tick++)
            {
                float fraction = tick / 4f;
                float y = plot.yMax - plot.height * fraction;
                Widgets.DrawLine(new Vector2(plot.x, y), new Vector2(plot.xMax, y), GridColor, 1f);
            }

            Vector2 prev = default;
            for (int i = 0; i < values.Length; i++)
            {
                float x = plot.x + plot.width * i / Mathf.Max(1, values.Length - 1);
                float y = plot.yMax - plot.height * (values[i] / yMax);
                if (i > 0)
                {
                    Widgets.DrawLine(prev, new Vector2(x, y), LineColor, 1f);
                }

                prev = new Vector2(x, y);
            }
        }

        private static void DrawYAxis(Rect axis, float yMax)
        {
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleRight;
            Color prev = GUI.color;
            GUI.color = new Color(0.7f, 0.7f, 0.7f);

            for (int tick = 1; tick <= 3; tick++)
            {
                float fraction = tick / 4f;
                float y = axis.yMax - axis.height * fraction;
                float value = yMax * fraction;
                Widgets.Label(new Rect(axis.x, y - 7f, axis.width - 2f, 14f), FormatAxisValue(value));
            }

            Widgets.Label(new Rect(axis.x, axis.y - 2f, axis.width - 2f, 14f), FormatAxisValue(yMax));

            GUI.color = prev;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private static string FormatAxisValue(float value)
        {
            if (value >= 10000f)
            {
                return (value / 1000f).ToString("0.#") + "k";
            }

            if (value >= 100f)
            {
                return value.ToString("0");
            }

            return value.ToString("0.#");
        }
    }

    public static class StackedAreaChart
    {
        public static void Draw(Rect rect, WorkHistoryTierBuffer history, int minHourIndex, System.Collections.Generic.List<Pawn> colonists)
        {
            Widgets.DrawBoxSolid(rect, new Color(0.1f, 0.1f, 0.1f, 0.35f));
            Text.Font = GameFont.Tiny;
            Widgets.Label(new Rect(rect.x + 4f, rect.y + 2f, rect.width, 18f), "WorkMonitor.MetricWorkAmong".Translate());

            if (history?.Buckets == null || history.Buckets.Count == 0 || colonists == null || colonists.Count == 0)
            {
                return;
            }

            Rect plot = new Rect(rect.x + 8f, rect.y + 22f, rect.width - 16f, rect.height - 30f);
            int colWidth = Mathf.Max(1, (int)(plot.width / history.Buckets.Count));
            int x = 0;
            foreach (var bucket in history.Buckets)
            {
                if (bucket.hourIndex < minHourIndex)
                {
                    continue;
                }

                int total = bucket.ticksSpent;
                if (total <= 0)
                {
                    x += colWidth;
                    continue;
                }

                float yBottom = plot.yMax;
                for (int i = 0; i < colonists.Count; i++)
                {
                    Pawn pawn = colonists[i];
                    if (!bucket.pawnTicksSpent.TryGetValue(pawn.thingIDNumber, out int pawnTicks) || pawnTicks <= 0)
                    {
                        continue;
                    }

                    float h = plot.height * (pawnTicks / (float)total);
                    Rect slice = new Rect(plot.x + x, yBottom - h, colWidth - 1f, h);
                    Widgets.DrawBoxSolid(slice, Color.HSVToRGB((i * 0.17f) % 1f, 0.55f, 0.85f));
                    yBottom -= h;
                }

                x += colWidth;
            }
        }
    }

    public struct PieSliceData
    {
        public string Label;
        public float Value;
        public Color Color;
        public float NewTodayValue;
        public int TravelTicks;
        public int WorkTicks;
    }

    public enum PieSliceSplitMode
    {
        None,
        WalkWork,
        NewTodayOlder
    }

    public enum PieValueFormat
    {
        Integer,
        WorkUnits,
        Duration
    }

    public static class PieChartPalette
    {
        public const float SwatchSize = 10f;
        public const float SwatchGap = 4f;
        public const float SwatchTotalWidth = SwatchSize + SwatchGap;

        private static readonly Dictionary<string, int> GroupIndexCache = new Dictionary<string, int>();

        public static Color ForWorkGroup(WorkGroupKey key)
        {
            return Color.HSVToRGB(ResolveGroupIndex(key) * 0.618f % 1f, 0.58f, 0.88f);
        }

        public static Color ForWorkGiver(int indexInList)
        {
            return Color.HSVToRGB(indexInList * 0.618f % 1f, 0.58f, 0.88f);
        }

        public static Color Lighten(Color baseColor)
        {
            return new Color(
                Mathf.Min(1f, baseColor.r + 0.28f),
                Mathf.Min(1f, baseColor.g + 0.28f),
                Mathf.Min(1f, baseColor.b + 0.28f),
                baseColor.a);
        }

        public static Color Darken(Color baseColor)
        {
            return new Color(baseColor.r * 0.62f, baseColor.g * 0.62f, baseColor.b * 0.62f, baseColor.a);
        }

        public static void DrawSwatch(Rect row, float left, Color? color)
        {
            Rect swatch = new Rect(left, row.y + (row.height - SwatchSize) * 0.5f, SwatchSize, SwatchSize);
            if (color.HasValue)
            {
                Widgets.DrawBoxSolid(swatch, color.Value);
            }
        }

        private static int ResolveGroupIndex(WorkGroupKey key)
        {
            string storageKey = key.StorageKey;
            if (GroupIndexCache.TryGetValue(storageKey, out int cached))
            {
                return cached;
            }

            int index = 0;
            foreach (WorkGroupSnapshot group in WorkGroupRegistry.GetAllGroups())
            {
                if (group.Key.StorageKey == storageKey)
                {
                    GroupIndexCache[storageKey] = index;
                    return index;
                }

                index++;
            }

            GroupIndexCache[storageKey] = index;
            return index;
        }
    }

    public static class DistributionPieChart
    {
        private static readonly Color WalkColor = new Color(0.58f, 0.58f, 0.62f);
        private static readonly Color WorkColor = new Color(0.4f, 0.85f, 0.5f);
        private static readonly Color SplitHintColor = new Color(0.75f, 0.75f, 0.75f);
        private static readonly Color SliceEdgeColor = new Color(0.06f, 0.06f, 0.06f, 0.95f);
        private const float SliceEdgeWidth = 1.25f;

        private struct PieArcHit
        {
            public float StartDeg;
            public float EndDeg;
            public float InnerRadiusFrac;
            public float OuterRadiusFrac;
            public string Tooltip;
        }

        public static void Draw(
            Rect rect,
            string title,
            List<PieSliceData> slices,
            PieSliceSplitMode splitMode,
            PieValueFormat valueFormat)
        {
            Widgets.DrawBoxSolid(rect, new Color(0.1f, 0.1f, 0.1f, 0.35f));
            Text.Font = GameFont.Tiny;
            Widgets.Label(new Rect(rect.x + 4f, rect.y + 2f, rect.width - 8f, 16f), title);

            float headerBottom = rect.y + 18f;
            if (splitMode == PieSliceSplitMode.WalkWork)
            {
                DrawSplitHint(new Rect(rect.x + 4f, rect.y + 16f, rect.width - 8f, 14f), WorkColor, WalkColor,
                    "WorkMonitor.PieSplitInner".Translate("WorkMonitor.WorkTime".Translate()),
                    "WorkMonitor.PieSplitOuter".Translate("WorkMonitor.Walk".Translate()));
                headerBottom = rect.y + 30f;
            }
            else if (splitMode == PieSliceSplitMode.NewTodayOlder)
            {
                DrawSplitHint(new Rect(rect.x + 4f, rect.y + 16f, rect.width - 8f, 14f),
                    PieChartPalette.Lighten(Color.white), PieChartPalette.Darken(Color.white),
                    "WorkMonitor.PieSplitInner".Translate("WorkMonitor.ChartNewJobToday".Translate()),
                    "WorkMonitor.PieSplitOuter".Translate("WorkMonitor.PieSplitOlder".Translate()));
                headerBottom = rect.y + 30f;
            }

            Rect plot = new Rect(rect.x + 8f, headerBottom + 4f, rect.width - 16f, rect.yMax - headerBottom - 8f);
            float total = 0f;
            if (slices != null)
            {
                foreach (PieSliceData slice in slices)
                {
                    if (slice.Value > 0f)
                    {
                        total += slice.Value;
                    }
                }
            }

            if (slices == null || slices.Count == 0 || total <= 0f)
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(plot, "-");
                Text.Anchor = TextAnchor.UpperLeft;
                return;
            }

            float radius = Mathf.Min(plot.width, plot.height) * 0.45f;
            Vector2 center = new Vector2(plot.x + plot.width * 0.5f, plot.y + plot.height * 0.5f);
            var hits = new List<PieArcHit>();
            float cursor = 0f;

            foreach (PieSliceData slice in slices)
            {
                if (slice.Value <= 0f)
                {
                    continue;
                }

                float sliceSpan = 360f * (slice.Value / total);
                float sliceEnd = cursor + sliceSpan;

                if (splitMode == PieSliceSplitMode.WalkWork)
                {
                    int travel = slice.TravelTicks;
                    int work = slice.WorkTicks;
                    int tickTotal = travel + work;
                    if (tickTotal <= 0)
                    {
                        AddSectorHit(hits, cursor, sliceEnd, 0f, 1f, slice.Label, FormatValue(slice.Value, valueFormat), null);
                        if (Event.current.type == EventType.Repaint)
                        {
                            DrawAnnulusSector(center, 0f, radius, cursor, sliceEnd, slice.Color);
                            DrawWedgeEdges(center, radius, cursor, sliceEnd, -1f);
                        }
                    }
                    else
                    {
                        float workFrac = work / (float)tickTotal;
                        DrawRadialSplit(
                            center,
                            radius,
                            cursor,
                            sliceEnd,
                            workFrac,
                            BlendSliceColor(slice.Color, WorkColor, 0.42f),
                            WalkColor,
                            hits,
                            slice.Label,
                            work,
                            travel,
                            valueFormat,
                            newTodayQualifier: "WorkMonitor.PieSliceHoverWork".Translate(),
                            outerQualifier: "WorkMonitor.PieSliceHoverWalk".Translate());
                    }
                }
                else if (splitMode == PieSliceSplitMode.NewTodayOlder)
                {
                    float newToday = Mathf.Clamp(slice.NewTodayValue, 0f, slice.Value);
                    float older = slice.Value - newToday;
                    float newFrac = newToday / slice.Value;
                    DrawRadialSplit(
                        center,
                        radius,
                        cursor,
                        sliceEnd,
                        newFrac,
                        PieChartPalette.Lighten(slice.Color),
                        PieChartPalette.Darken(slice.Color),
                        hits,
                        slice.Label,
                        newToday,
                        older,
                        valueFormat,
                        newTodayQualifier: "WorkMonitor.PieSliceHoverNewToday".Translate(),
                        outerQualifier: "WorkMonitor.PieSliceHoverOlder".Translate());
                }
                else
                {
                    AddSectorHit(hits, cursor, sliceEnd, 0f, 1f, slice.Label, FormatValue(slice.Value, valueFormat), null);
                    if (Event.current.type == EventType.Repaint)
                    {
                        DrawAnnulusSector(center, 0f, radius, cursor, sliceEnd, slice.Color);
                        DrawWedgeEdges(center, radius, cursor, sliceEnd, -1f);
                    }
                }

                cursor = sliceEnd;
            }

            if (plot.Contains(Event.current.mousePosition))
            {
                string tip = HitTest(hits, center, radius, Event.current.mousePosition);
                if (!tip.NullOrEmpty())
                {
                    TooltipHandler.TipRegion(plot, tip);
                }
            }
        }

        private static void DrawRadialSplit(
            Vector2 center,
            float radius,
            float startDeg,
            float endDeg,
            float innerPortion,
            Color innerColor,
            Color outerColor,
            List<PieArcHit> hits,
            string label,
            float innerValue,
            float outerValue,
            PieValueFormat valueFormat,
            string newTodayQualifier = null,
            string outerQualifier = null)
        {
            innerPortion = Mathf.Clamp01(innerPortion);
            string innerQual = newTodayQualifier ?? "WorkMonitor.PieSliceHoverWalk".Translate();
            string outerQual = outerQualifier ?? "WorkMonitor.PieSliceHoverWork".Translate();
            float innerR = radius * innerPortion;

            if (innerPortion > 0.001f)
            {
                AddSectorHit(hits, startDeg, endDeg, 0f, innerPortion, label, FormatValue(innerValue, valueFormat), innerQual);
                if (Event.current.type == EventType.Repaint)
                {
                    DrawAnnulusSector(center, 0f, innerR, startDeg, endDeg, innerColor);
                }
            }

            if (innerPortion < 0.999f)
            {
                AddSectorHit(hits, startDeg, endDeg, innerPortion, 1f, label, FormatValue(outerValue, valueFormat), outerQual);
                if (Event.current.type == EventType.Repaint)
                {
                    DrawAnnulusSector(center, innerR, radius, startDeg, endDeg, outerColor);
                }
            }

            if (Event.current.type == EventType.Repaint)
            {
                DrawWedgeEdges(center, radius, startDeg, endDeg, innerPortion);
            }
        }

        private static Color BlendSliceColor(Color sliceColor, Color accent, float accentWeight)
        {
            float w = Mathf.Clamp01(accentWeight);
            return new Color(
                Mathf.Lerp(sliceColor.r, accent.r, w),
                Mathf.Lerp(sliceColor.g, accent.g, w),
                Mathf.Lerp(sliceColor.b, accent.b, w),
                sliceColor.a);
        }

        private static void DrawWedgeEdges(
            Vector2 center,
            float radius,
            float startDeg,
            float endDeg,
            float innerPortionFrac)
        {
            DrawRadialEdge(center, 0f, radius, startDeg);
            DrawRadialEdge(center, 0f, radius, endDeg);
            DrawArcEdge(center, radius, startDeg, endDeg);

            if (innerPortionFrac > 0.001f && innerPortionFrac < 0.999f)
            {
                DrawArcEdge(center, radius * innerPortionFrac, startDeg, endDeg);
            }
        }

        private static void DrawRadialEdge(Vector2 center, float innerRadius, float outerRadius, float clockwiseDeg)
        {
            Vector2 inner = PointOnCircle(center, innerRadius, clockwiseDeg);
            Vector2 outer = PointOnCircle(center, outerRadius, clockwiseDeg);
            Widgets.DrawLine(inner, outer, SliceEdgeColor, SliceEdgeWidth);
        }

        private static void DrawArcEdge(Vector2 center, float arcRadius, float startDeg, float endDeg)
        {
            float span = endDeg - startDeg;
            if (span <= 0.001f || arcRadius <= 0.001f)
            {
                return;
            }

            int segments = Mathf.Clamp(Mathf.CeilToInt(span / 3f), 2, 120);
            float step = span / segments;
            Vector2 prev = PointOnCircle(center, arcRadius, startDeg);
            for (int i = 1; i <= segments; i++)
            {
                float deg = startDeg + step * i;
                Vector2 next = PointOnCircle(center, arcRadius, deg);
                Widgets.DrawLine(prev, next, SliceEdgeColor, SliceEdgeWidth);
                prev = next;
            }
        }

        private static void AddSectorHit(
            List<PieArcHit> hits,
            float startDeg,
            float endDeg,
            float innerRadiusFrac,
            float outerRadiusFrac,
            string label,
            string value,
            string qualifier)
        {
            hits.Add(new PieArcHit
            {
                StartDeg = startDeg,
                EndDeg = endDeg,
                InnerRadiusFrac = innerRadiusFrac,
                OuterRadiusFrac = outerRadiusFrac,
                Tooltip = FormatHover(label, value, qualifier)
            });
        }

        private static void DrawSplitHint(Rect rect, Color leftColor, Color rightColor, string leftLabel, string rightLabel)
        {
            Color prev = GUI.color;
            Widgets.DrawBoxSolid(new Rect(rect.x, rect.y + 3f, 8f, 8f), leftColor);
            GUI.color = SplitHintColor;
            float x = rect.x + 12f;
            Widgets.Label(new Rect(x, rect.y, 60f, rect.height), leftLabel);
            x += 52f;
            Widgets.Label(new Rect(x, rect.y, 8f, rect.height), "\u25AA");
            x += 10f;
            Widgets.DrawBoxSolid(new Rect(x, rect.y + 3f, 8f, 8f), rightColor);
            x += 12f;
            Widgets.Label(new Rect(x, rect.y, rect.width - (x - rect.x), rect.height), rightLabel);
            GUI.color = prev;
        }

        private static string FormatHover(string label, string value, string qualifier)
        {
            string item = qualifier.NullOrEmpty() ? label : label + " (" + qualifier + ")";
            return "WorkMonitor.PieSliceHover".Translate(item, value);
        }

        private static string FormatValue(float value, PieValueFormat format)
        {
            return format switch
            {
                PieValueFormat.WorkUnits => WorkMonitorUtility.FormatWorkUnits(value),
                PieValueFormat.Duration => WorkMonitorUtility.FormatDuration(
                    Mathf.RoundToInt(value),
                    WorkMonitorMod.Settings?.showTimeInHours ?? true),
                _ => Mathf.RoundToInt(value).ToString()
            };
        }

        private static string HitTest(List<PieArcHit> hits, Vector2 center, float radius, Vector2 mouse)
        {
            Vector2 local = mouse - center;
            float dist = local.magnitude;
            if (dist > radius || radius <= 0f)
            {
                return null;
            }

            float distFrac = dist / radius;
            float clockwise = ClockwiseDegreesFromTop(local);
            foreach (PieArcHit hit in hits)
            {
                if (clockwise >= hit.StartDeg
                    && clockwise < hit.EndDeg
                    && distFrac >= hit.InnerRadiusFrac
                    && distFrac < hit.OuterRadiusFrac)
                {
                    return hit.Tooltip;
                }
            }

            return null;
        }

        private static float ClockwiseDegreesFromTop(Vector2 local)
        {
            float ang = Mathf.Atan2(local.y, local.x) * Mathf.Rad2Deg;
            return (ang + 90f + 360f) % 360f;
        }

        private static Vector2 PointOnCircle(Vector2 center, float radius, float clockwiseFromTop)
        {
            float rad = (-90f + clockwiseFromTop) * Mathf.Deg2Rad;
            return new Vector2(
                center.x + Mathf.Cos(rad) * radius,
                center.y + Mathf.Sin(rad) * radius);
        }

        private static void DrawAnnulusSector(
            Vector2 center,
            float innerRadius,
            float outerRadius,
            float startClockwise,
            float endClockwise,
            Color color)
        {
            float span = endClockwise - startClockwise;
            if (span <= 0.001f || outerRadius <= innerRadius + 0.001f)
            {
                return;
            }

            int segments = Mathf.Clamp(Mathf.CeilToInt(span / 2f), 1, 180);
            float step = span / segments;
            Material mat = SolidColorMaterials.SimpleSolidColorMaterial(color);
            mat.SetPass(0);

            GL.PushMatrix();
            GL.MultMatrix(GUI.matrix);
            GL.Begin(GL.TRIANGLES);
            GL.Color(color);

            for (int i = 0; i < segments; i++)
            {
                float a0 = startClockwise + step * i;
                float a1 = startClockwise + step * (i + 1);
                Vector2 p0Inner = PointOnCircle(center, innerRadius, a0);
                Vector2 p1Inner = PointOnCircle(center, innerRadius, a1);
                Vector2 p0Outer = PointOnCircle(center, outerRadius, a0);
                Vector2 p1Outer = PointOnCircle(center, outerRadius, a1);

                GL.Vertex3(p0Inner.x, p0Inner.y, 0f);
                GL.Vertex3(p0Outer.x, p0Outer.y, 0f);
                GL.Vertex3(p1Outer.x, p1Outer.y, 0f);

                GL.Vertex3(p0Inner.x, p0Inner.y, 0f);
                GL.Vertex3(p1Outer.x, p1Outer.y, 0f);
                GL.Vertex3(p1Inner.x, p1Inner.y, 0f);
            }

            GL.End();
            GL.PopMatrix();
        }
    }
}
