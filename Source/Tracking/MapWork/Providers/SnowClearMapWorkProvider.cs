using System.Collections.Generic;
using RimWorld;
using Verse;

namespace WorkMonitor.Tracking.MapWork.Providers
{
    public class SnowClearMapWorkProvider : IMapWorkTargetProvider
    {
        public void Collect(Map map, List<ScannedMapTarget> targets)
        {
            WorkGiverDef workGiver = MapWorkAttribution.GetWorkGiver("CleanClearSnow");
            if (workGiver == null || map.areaManager.Home == null)
            {
                return;
            }

            List<WorkGiverDef> workGivers = new List<WorkGiverDef> { workGiver };

            foreach (IntVec3 cell in map.areaManager.Home.ActiveCells)
            {
                if (!cell.InBounds(map))
                {
                    continue;
                }

                float depth = map.snowGrid.GetDepth(cell);
                if (depth <= 0.01f)
                {
                    continue;
                }

                targets.Add(new ScannedMapTarget(
                    "snow:" + cell,
                    depth * 100f,
                    workGivers));
            }
        }
    }
}
