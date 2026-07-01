using System.Collections.Generic;
using UnityEngine;
using Verse;
using WorkMonitor.Groups;

namespace WorkMonitor.UI
{
    public class WorkGroupMapBacklogPieChartPanel
    {
        private const float Gap = 8f;

        public void Draw(Rect rect, WorkGroupStats stats)
        {
            if (stats?.WorkGiverStats == null)
            {
                return;
            }

            float cellW = (rect.width - Gap) / 2f;
            List<PieSliceData> taskSlices = BuildTaskSlices(stats.WorkGiverStats);
            List<PieSliceData> workSlices = BuildWorkSlices(stats.WorkGiverStats);

            DistributionPieChart.Draw(
                new Rect(rect.x, rect.y, cellW, rect.height),
                "WorkMonitor.MetricJobCount".Translate(),
                taskSlices,
                PieSliceSplitMode.NewTodayOlder,
                PieValueFormat.Integer);

            DistributionPieChart.Draw(
                new Rect(rect.x + cellW + Gap, rect.y, cellW, rect.height),
                "WorkMonitor.MetricWorkUnits".Translate(),
                workSlices,
                PieSliceSplitMode.NewTodayOlder,
                PieValueFormat.WorkUnits);
        }

        private static List<PieSliceData> BuildTaskSlices(List<WorkGiverStat> workGivers)
        {
            var slices = new List<PieSliceData>();
            for (int i = 0; i < workGivers.Count; i++)
            {
                WorkGiverStat wg = workGivers[i];
                if (wg.MapOpenTasks <= 0)
                {
                    continue;
                }

                slices.Add(new PieSliceData
                {
                    Label = wg.Label,
                    Value = wg.MapOpenTasks,
                    NewTodayValue = wg.MapNewTodayOpenTasks,
                    Color = PieChartPalette.ForWorkGiver(i)
                });
            }

            return slices;
        }

        private static List<PieSliceData> BuildWorkSlices(List<WorkGiverStat> workGivers)
        {
            var slices = new List<PieSliceData>();
            for (int i = 0; i < workGivers.Count; i++)
            {
                WorkGiverStat wg = workGivers[i];
                if (wg.MapWorkLeft <= 0f)
                {
                    continue;
                }

                slices.Add(new PieSliceData
                {
                    Label = wg.Label,
                    Value = wg.MapWorkLeft,
                    NewTodayValue = wg.MapNewTodayWorkLeft,
                    Color = PieChartPalette.ForWorkGiver(i)
                });
            }

            return slices;
        }
    }
}
