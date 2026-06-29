using System.Collections.Generic;
using RimWorld;
using Verse;

namespace WorkMonitor.Tracking.MapWork.Providers
{
    public class ListerHaulablesMapWorkProvider : IMapWorkTargetProvider
    {
        public void Collect(Map map, List<ScannedMapTarget> targets)
        {
            WorkGiverDef haulGeneral = MapWorkAttribution.GetWorkGiver("HaulGeneral");
            WorkGiverDef haulCorpses = MapWorkAttribution.GetWorkGiver("HaulCorpses");
            if (haulGeneral == null && haulCorpses == null)
            {
                return;
            }

            foreach (Thing thing in map.listerHaulables.ThingsPotentiallyNeedingHauling())
            {
                if (thing == null || !thing.Spawned)
                {
                    continue;
                }

                bool isCorpse = thing is Corpse;
                WorkGiverDef workGiver = isCorpse ? haulCorpses : haulGeneral;
                if (workGiver == null)
                {
                    continue;
                }

                targets.Add(new ScannedMapTarget(
                    "haul:" + thing.thingIDNumber,
                    0f,
                    new List<WorkGiverDef> { workGiver }));
            }
        }
    }
}
