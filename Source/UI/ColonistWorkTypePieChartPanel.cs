using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using WorkMonitor.Groups;

namespace WorkMonitor.UI
{
    public class ColonistWorkTypePieChartPanel
    {
        private const float Gap = 8f;

        public void Draw(Rect rect, ColonistStats stats)
        {
            if (stats == null)
            {
                return;
            }

            float cellW = (rect.width - Gap * 2f) / 3f;
            float y = rect.y;
            float h = rect.height;

            DrawPie(
                new Rect(rect.x, y, cellW, h),
                "WorkMonitor.MetricJobCount".Translate(),
                BuildJobSlices(stats),
                PieSliceSplitMode.None,
                PieValueFormat.Integer);

            DrawPie(
                new Rect(rect.x + cellW + Gap, y, cellW, h),
                "WorkMonitor.MetricWorkUnits".Translate(),
                BuildWorkSlices(stats),
                PieSliceSplitMode.None,
                PieValueFormat.WorkUnits);

            DrawPie(
                new Rect(rect.x + (cellW + Gap) * 2f, y, cellW, h),
                "WorkMonitor.MetricTimeConsumed".Translate(),
                BuildTimeSlices(stats),
                PieSliceSplitMode.WalkWork,
                PieValueFormat.Duration);
        }

        private static void DrawPie(Rect rect, string title, List<PieSliceData> slices, PieSliceSplitMode splitMode, PieValueFormat format)
        {
            DistributionPieChart.Draw(rect, title, slices, splitMode, format);
        }

        private static List<PieSliceData> BuildJobSlices(ColonistStats stats)
        {
            return stats.GroupStats
                .Where(g => g.JobCount > 0)
                .OrderByDescending(g => g.JobCount)
                .Select(g => new PieSliceData
                {
                    Label = g.Group.Label,
                    Value = g.JobCount,
                    Color = PieChartPalette.ForWorkGroup(g.Group.Key)
                })
                .ToList();
        }

        private static List<PieSliceData> BuildWorkSlices(ColonistStats stats)
        {
            return stats.GroupStats
                .Where(g => g.WorkUnitsSpent > 0f)
                .OrderByDescending(g => g.WorkUnitsSpent)
                .Select(g => new PieSliceData
                {
                    Label = g.Group.Label,
                    Value = g.WorkUnitsSpent,
                    Color = PieChartPalette.ForWorkGroup(g.Group.Key)
                })
                .ToList();
        }

        private static List<PieSliceData> BuildTimeSlices(ColonistStats stats)
        {
            return stats.GroupStats
                .Where(g => g.TicksSpent > 0)
                .OrderByDescending(g => g.TicksSpent)
                .Select(g => new PieSliceData
                {
                    Label = g.Group.Label,
                    Value = g.TicksSpent,
                    Color = PieChartPalette.ForWorkGroup(g.Group.Key),
                    TravelTicks = g.TravelTicksSpent,
                    WorkTicks = g.WorkTicksSpent
                })
                .ToList();
        }
    }
}
