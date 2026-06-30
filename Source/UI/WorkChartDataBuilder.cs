using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
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
            WorkHistoryRingBuffer history,
            int minHourIndex,
            int rangeHours,
            out float[] values,
            out string[] labels)
        {
            BuildFixedHourlySeries(
                history,
                minHourIndex,
                rangeHours,
                bucket => bucket != null ? bucket.workUnitsSpent + bucket.estimatedWorkUnitsSpent : 0f,
                out values,
                out labels);
        }

        public static void BuildJobCountSeries(
            WorkHistoryRingBuffer history,
            int minHourIndex,
            int rangeHours,
            out float[] values,
            out string[] labels)
        {
            BuildFixedHourlySeries(
                history,
                minHourIndex,
                rangeHours,
                bucket => bucket != null ? bucket.jobCount : 0f,
                out values,
                out labels);
        }

        public static void BuildMapOpenTasksSeries(
            WorkHistoryRingBuffer colonistHistory,
            string groupStorageKey,
            int minHourIndex,
            int rangeHours,
            out float[] values,
            out string[] labels)
        {
            BuildMapMetricSeries(colonistHistory, minHourIndex, rangeHours, groupStorageKey, map: true, out values, out labels);
        }

        public static void BuildMapWorkLeftSeries(
            WorkHistoryRingBuffer colonistHistory,
            string groupStorageKey,
            int minHourIndex,
            int rangeHours,
            out float[] values,
            out string[] labels)
        {
            BuildMapMetricSeries(colonistHistory, minHourIndex, rangeHours, groupStorageKey, map: false, out values, out labels);
        }

        private static void BuildFixedHourlySeries(
            WorkHistoryRingBuffer history,
            int minHourIndex,
            int rangeHours,
            System.Func<HourlyWorkBucket, float> selector,
            out float[] values,
            out string[] labels)
        {
            values = new float[rangeHours];
            labels = new string[rangeHours];
            for (int i = 0; i < rangeHours; i++)
            {
                int hour = minHourIndex + i;
                HourlyWorkBucket bucket = history?.GetBucket(hour);
                values[i] = selector(bucket);
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

        private static void BuildMapMetricSeries(
            WorkHistoryRingBuffer colonistHistory,
            int minHourIndex,
            int rangeHours,
            string groupStorageKey,
            bool map,
            out float[] values,
            out string[] labels)
        {
            values = new float[rangeHours];
            labels = new string[rangeHours];

            MapWorkSampler sampler = MapWorkSampler.EnsureRegistered();
            IReadOnlyList<MapWorkSnapshot> mapHistory = sampler?.GetHistory();
            float lastValue = 0f;
            int mapIndex = 0;

            for (int i = 0; i < rangeHours; i++)
            {
                int hour = minHourIndex + i;
                labels[i] = BuildRelativeHourLabel(i, rangeHours);

                if (mapHistory != null)
                {
                    while (mapIndex < mapHistory.Count && mapHistory[mapIndex].hourIndex <= hour)
                    {
                        if (mapHistory[mapIndex].perGroupKey.TryGetValue(groupStorageKey, out MapWorkGroupSnapshot groupSnap))
                        {
                            lastValue = map ? groupSnap.openTaskCount : groupSnap.workLeftTotal;
                        }

                        mapIndex++;
                    }
                }

                values[i] = lastValue;
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
            Widgets.DrawBoxSolid(rect, new Color(0.1f, 0.1f, 0.1f, 0.35f));
            Text.Font = GameFont.Tiny;
            Widgets.Label(new Rect(rect.x + 4f, rect.y + 2f, rect.width - 8f, 16f), title);

            float legendY = rect.y + 18f;
            DrawLegendEntry(new Rect(rect.x + 6f, legendY, rect.width * 0.5f - 8f, 14f), ColonistColor, colonistLegend);
            DrawLegendEntry(new Rect(rect.x + rect.width * 0.5f, legendY, rect.width * 0.5f - 6f, 14f), MapColor, mapLegend);

            const float axisWidth = 30f;
            const float xAxisHeight = 16f;
            Rect plot = new Rect(rect.x + axisWidth + 4f, rect.y + 34f, rect.width - axisWidth - 12f, rect.height - 40f - xAxisHeight);
            if (colonistValues == null || colonistValues.Length == 0)
            {
                Widgets.Label(plot, "-");
                return;
            }

            float yMax = ComputeYMax(colonistValues, mapValues, stacked: false);
            DrawYAxis(new Rect(rect.x + 2f, plot.y, axisWidth, plot.height), yMax);
            DrawGrid(plot, yMax);

            DrawSeries(plot, colonistValues, yMax, ColonistColor);
            if (mapValues != null && mapValues.Length == colonistValues.Length)
            {
                DrawSeries(plot, mapValues, yMax, MapColor);
            }

            ChartAxisHelper.DrawXAxisLabels(plot, xLabels);
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
            Widgets.DrawBoxSolid(rect, new Color(0.1f, 0.1f, 0.1f, 0.35f));
            Text.Font = GameFont.Tiny;
            Widgets.Label(new Rect(rect.x + 4f, rect.y + 2f, rect.width - 8f, 16f), title);

            float legendY = rect.y + 18f;
            DrawLegendEntry(new Rect(rect.x + 6f, legendY, rect.width * 0.5f - 8f, 14f), ColonistColor, colonistLegend);
            DrawLegendEntry(new Rect(rect.x + rect.width * 0.5f, legendY, rect.width * 0.5f - 6f, 14f), MapColor, mapLegend);

            const float axisWidth = 30f;
            const float xAxisHeight = 16f;
            Rect plot = new Rect(rect.x + axisWidth + 4f, rect.y + 34f, rect.width - axisWidth - 12f, rect.height - 40f - xAxisHeight);
            if (colonistValues == null || colonistValues.Length == 0)
            {
                Widgets.Label(plot, "-");
                return;
            }

            float yMax = ComputeStackedYMax(colonistValues, mapValues);
            DrawYAxis(new Rect(rect.x + 2f, plot.y, axisWidth, plot.height), yMax);

            for (int tick = 1; tick <= 3; tick++)
            {
                float fraction = tick / 4f;
                float y = plot.yMax - plot.height * fraction;
                Widgets.DrawLine(new Vector2(plot.x, y), new Vector2(plot.xMax, y), GridColor, 1f);
            }

            int count = colonistValues.Length;
            float colWidth = plot.width / Mathf.Max(1, count);
            for (int i = 0; i < count; i++)
            {
                float mapValue = mapValues != null && i < mapValues.Length ? mapValues[i] : 0f;
                float colonistValue = colonistValues[i];
                float mapHeight = plot.height * (mapValue / yMax);
                float colonistHeight = plot.height * (colonistValue / yMax);
                float x = plot.x + i * colWidth;
                float sliceWidth = Mathf.Max(1f, colWidth - 1f);

                if (mapHeight > 0f)
                {
                    Widgets.DrawBoxSolid(new Rect(x, plot.yMax - mapHeight, sliceWidth, mapHeight), MapColor);
                }

                if (colonistHeight > 0f)
                {
                    Widgets.DrawBoxSolid(new Rect(x, plot.yMax - mapHeight - colonistHeight, sliceWidth, colonistHeight), ColonistColor);
                }
            }

            ChartAxisHelper.DrawXAxisLabels(plot, xLabels);
        }

        private static float ComputeStackedYMax(float[] colonistValues, float[] mapValues)
        {
            float yMax = 0.01f;
            for (int i = 0; i < colonistValues.Length; i++)
            {
                float mapValue = mapValues != null && i < mapValues.Length ? mapValues[i] : 0f;
                float total = colonistValues[i] + mapValue;
                if (total > yMax)
                {
                    yMax = total;
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
        public static void Draw(Rect rect, WorkHistoryRingBuffer history, int minHourIndex, System.Collections.Generic.List<Pawn> colonists)
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
}
