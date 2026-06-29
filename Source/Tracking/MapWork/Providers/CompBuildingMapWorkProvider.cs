using System.Collections.Generic;
using RimWorld;
using Verse;

namespace WorkMonitor.Tracking.MapWork.Providers
{
    public class CompBuildingMapWorkProvider : IMapWorkTargetProvider
    {
        public void Collect(Map map, List<ScannedMapTarget> targets)
        {
            WorkGiverDef drillWorkGiver = MapWorkAttribution.DrillWorkGiver();
            if (drillWorkGiver == null)
            {
                return;
            }

            List<WorkGiverDef> drillGivers = new List<WorkGiverDef> { drillWorkGiver };

            foreach (Thing thing in map.listerThings.AllThings)
            {
                CompDeepDrill drill = thing?.TryGetComp<CompDeepDrill>();
                if (drill == null || !thing.Spawned)
                {
                    continue;
                }

                if (!drill.CanDrillNow())
                {
                    continue;
                }

                targets.Add(new ScannedMapTarget(
                    "drill:" + thing.thingIDNumber,
                    0f,
                    drillGivers));
            }
        }
    }
}
