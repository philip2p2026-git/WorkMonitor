using UnityEngine;
using Verse;
using WorkMonitor.Tracking;

namespace WorkMonitor.UI
{
    public enum WorkChartMetric
    {
        JobCount,
        TimeConsumed,
        WorkAmong,
        RelativeShare
    }

    public static class WorkChartDataBuilder
    {
        public static void BuildLineSeries(
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

                values[i] = b.ticksSpent / (float)WorkMonitorSettings.TicksPerHour;
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

        public static void Draw(Rect rect, float[] values, string title)
        {
            Widgets.DrawBoxSolid(rect, new Color(0.1f, 0.1f, 0.1f, 0.35f));
            Text.Font = GameFont.Tiny;
            Widgets.Label(new Rect(rect.x + 4f, rect.y + 2f, rect.width, 18f), title);

            Rect plot = new Rect(rect.x + 8f, rect.y + 22f, rect.width - 16f, rect.height - 30f);
            if (values == null || values.Length == 0)
            {
                Widgets.Label(plot, "-");
                return;
            }

            float max = 0.01f;
            foreach (float v in values)
            {
                if (v > max)
                {
                    max = v;
                }
            }

            Vector2 prev = default;
            for (int i = 0; i < values.Length; i++)
            {
                float x = plot.x + plot.width * i / Mathf.Max(1, values.Length - 1);
                float y = plot.yMax - plot.height * (values[i] / max);
                if (i > 0)
                {
                    Widgets.DrawLine(prev, new Vector2(x, y), LineColor, 1f);
                }

                prev = new Vector2(x, y);
            }
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
