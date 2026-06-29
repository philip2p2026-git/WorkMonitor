using RimWorld;
using UnityEngine;
using Verse;
using WorkMonitor.Tracking;

namespace WorkMonitor.UI
{
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
            out float[] values,
            out string[] labels)
        {
            var buckets = history?.Buckets;
            if (buckets == null || buckets.Count == 0)
            {
                values = new float[0];
                labels = new string[0];
                return;
            }

            int count = 0;
            foreach (var b in buckets)
            {
                if (b.hourIndex >= minHourIndex)
                {
                    count++;
                }
            }

            values = new float[count];
            labels = new string[count];
            int i = 0;
            foreach (var b in buckets)
            {
                if (b.hourIndex < minHourIndex)
                {
                    continue;
                }

                values[i] = b.workUnitsSpent;
                labels[i] = (b.hourIndex % 24).ToString() + "h";
                i++;
            }
        }

        public static void BuildJobCountSeries(
            WorkHistoryRingBuffer history,
            int minHourIndex,
            out float[] values,
            out string[] labels)
        {
            var buckets = history?.Buckets;
            if (buckets == null || buckets.Count == 0)
            {
                values = new float[0];
                labels = new string[0];
                return;
            }

            int count = 0;
            foreach (var b in buckets)
            {
                if (b.hourIndex >= minHourIndex)
                {
                    count++;
                }
            }

            values = new float[count];
            labels = new string[count];
            int i = 0;
            foreach (var b in buckets)
            {
                if (b.hourIndex < minHourIndex)
                {
                    continue;
                }

                values[i] = b.jobCount;
                labels[i] = (b.hourIndex % 24).ToString() + "h";
                i++;
            }
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
