using System.Collections.Generic;
using RimWorld;
using Verse;

namespace WorkMonitor.Tracking.MapWork.Providers
{
    public class BrokenDownBuildingMapWorkProvider : IMapWorkTargetProvider
    {
        public void Collect(Map map, List<ScannedMapTarget> targets)
        {
            WorkGiverDef workGiver = MapWorkAttribution.GetWorkGiver("FixBrokenDownBuilding");
            if (workGiver == null)
            {
                return;
            }

            List<WorkGiverDef> workGivers = new List<WorkGiverDef> { workGiver };

            foreach (Thing thing in map.listerThings.AllThings)
            {
                CompBreakdownable breakdownable = thing?.TryGetComp<CompBreakdownable>();
                if (breakdownable == null || !breakdownable.BrokenDown)
                {
                    continue;
                }

                targets.Add(new ScannedMapTarget(
                    "breakdown:" + thing.thingIDNumber,
                    MapWorkEstimate.FromThingDeconstruct(thing),
                    workGivers));
            }
        }
    }
}
