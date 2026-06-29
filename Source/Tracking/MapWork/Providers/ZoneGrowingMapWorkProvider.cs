using System.Collections.Generic;
using RimWorld;
using Verse;

namespace WorkMonitor.Tracking.MapWork.Providers
{
    public class ZoneGrowingMapWorkProvider : IMapWorkTargetProvider
    {
        public void Collect(Map map, List<ScannedMapTarget> targets)
        {
            WorkGiverDef harvest = MapWorkAttribution.GetWorkGiver("GrowerHarvest");
            WorkGiverDef sow = MapWorkAttribution.GetWorkGiver("GrowerSow");
            if (harvest == null && sow == null)
            {
                return;
            }

            foreach (Zone zone in map.zoneManager.AllZones)
            {
                if (zone is not Zone_Growing growing)
                {
                    continue;
                }

                foreach (IntVec3 cell in growing.Cells)
                {
                    if (!cell.InBounds(map))
                    {
                        continue;
                    }

                    Plant plant = cell.GetPlant(map);
                    if (plant != null && harvest != null && plant.HarvestableNow)
                    {
                        targets.Add(new ScannedMapTarget(
                            "growharvest:" + cell,
                            MapWorkEstimate.FromPlantCut(plant),
                            new List<WorkGiverDef> { harvest }));
                        continue;
                    }

                    if (plant == null && sow != null && WantsSow(map, cell, growing))
                    {
                        ThingDef plantDef = growing.GetPlantDefToGrow();
                        float work = plantDef?.plant != null ? plantDef.plant.sowWork : 0f;
                        targets.Add(new ScannedMapTarget(
                            "growsow:" + cell,
                            work,
                            new List<WorkGiverDef> { sow }));
                    }
                }
            }
        }

        private static bool WantsSow(Map map, IntVec3 cell, Zone_Growing growing)
        {
            if (!cell.Standable(map) || cell.Fogged(map))
            {
                return false;
            }

            return growing.GetPlantDefToGrow() != null;
        }
    }
}
