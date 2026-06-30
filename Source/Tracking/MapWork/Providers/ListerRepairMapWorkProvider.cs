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

            Faction playerFaction = Faction.OfPlayer;
            if (playerFaction == null)
            {
                return;
            }

            List<Thing> repairables = map.listerBuildingsRepairable.RepairableBuildings(playerFaction);
            if (repairables == null || repairables.Count == 0)
            {
                return;
            }

            List<WorkGiverDef> workGivers = new List<WorkGiverDef> { workGiver };

            foreach (Thing thing in repairables)
            {
                if (thing == null || !thing.Spawned || thing.Destroyed)
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
