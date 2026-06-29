using System.Collections.Generic;
using RimWorld;
using Verse;

namespace WorkMonitor.Tracking.MapWork.Providers
{
    public class ListerRepairMapWorkProvider : IMapWorkTargetProvider
    {
        public void Collect(Map map, List<ScannedMapTarget> targets)
        {
            WorkGiverDef workGiver = MapWorkAttribution.GetWorkGiver("Repair");
            if (workGiver == null)
            {
                return;
            }

            List<WorkGiverDef> workGivers = new List<WorkGiverDef> { workGiver };
            HashSet<int> seen = new HashSet<int>();

            foreach (Thing thing in map.listerThings.AllThings)
            {
                if (thing == null || !thing.Spawned || !thing.def.useHitPoints || thing.Destroyed)
                {
                    continue;
                }

                if (thing.HitPoints >= thing.MaxHitPoints)
                {
                    continue;
                }

                if (!thing.def.building?.repairable ?? true)
                {
                    continue;
                }

                if (!seen.Add(thing.thingIDNumber))
                {
                    continue;
                }

                float work = MapWorkEstimate.FromRepair(thing);
                if (work <= 0f)
                {
                    continue;
                }

                targets.Add(new ScannedMapTarget(
                    "repair:" + thing.thingIDNumber,
                    work,
                    workGivers));
            }
        }
    }
}
