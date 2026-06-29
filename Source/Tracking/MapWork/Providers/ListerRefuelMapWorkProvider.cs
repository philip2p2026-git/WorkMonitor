using System.Collections.Generic;
using RimWorld;
using Verse;

namespace WorkMonitor.Tracking.MapWork.Providers
{
    public class ListerRefuelMapWorkProvider : IMapWorkTargetProvider
    {
        public void Collect(Map map, List<ScannedMapTarget> targets)
        {
            WorkGiverDef refuel = MapWorkAttribution.GetWorkGiver("Refuel");
            WorkGiverDef rearm = MapWorkAttribution.GetWorkGiver("RearmTurrets");
            HashSet<int> seen = new HashSet<int>();

            foreach (Thing thing in map.listerThings.AllThings)
            {
                if (thing == null || !thing.Spawned || !seen.Add(thing.thingIDNumber))
                {
                    continue;
                }

                CompRefuelable refuelable = thing.TryGetComp<CompRefuelable>();
                if (refuelable == null || refuelable.HasFuel)
                {
                    continue;
                }

                WorkGiverDef workGiver = thing.def.building?.IsTurret == true ? rearm : refuel;
                if (workGiver == null)
                {
                    workGiver = refuel ?? rearm;
                }

                if (workGiver == null)
                {
                    continue;
                }

                targets.Add(new ScannedMapTarget(
                    "refuel:" + thing.thingIDNumber,
                    0f,
                    new List<WorkGiverDef> { workGiver }));
            }
        }
    }
}
