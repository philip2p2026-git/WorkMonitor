using RimWorld;
using Verse;
using Verse.AI;

namespace WorkMonitor.Tracking.MapWork
{
    /// <summary>
    /// Mirrors WorkGiver_GrowerSow eligibility for map backlog scans (colonist-equivalent sow targets).
    /// </summary>
    public static class GrowerSowMapUtility
    {
        public static bool TryGetSowWork(Map map, IntVec3 cell, Zone_Growing zone, out float sowWork, out ThingDef plantDef)
        {
            sowWork = 0f;
            plantDef = null;

            if (map == null || zone == null || !cell.InBounds(map))
            {
                return false;
            }

            if (!zone.allowSow || !zone.CanAcceptSowNow())
            {
                return false;
            }

            if (cell.Fogged(map) || !cell.Standable(map))
            {
                return false;
            }

            plantDef = WorkGiver_Grower.CalculateWantedPlantDef(cell, map) ?? zone.GetPlantDefToGrow();
            if (plantDef?.plant == null)
            {
                return false;
            }

            if (!PlantUtility.GrowthSeasonNow(cell, map, plantDef))
            {
                return false;
            }

            if (CellHasPlantOfDef(cell, map, plantDef))
            {
                return false;
            }

            if (!PassesBlueprintAndFertilityChecks(cell, map))
            {
                return false;
            }

            if (!PassesCavePlantRules(cell, map, plantDef))
            {
                return false;
            }

            if (!PassesRoofRules(cell, map, plantDef))
            {
                return false;
            }

            Plant existingPlant = cell.GetPlant(map);
            if (existingPlant != null && existingPlant.def.plant.blockAdjacentSow)
            {
                return false;
            }

            if (PlantUtility.AdjacentSowBlocker(plantDef, cell, map) != null)
            {
                return false;
            }

            if (CellHasBlockingThing(cell, map))
            {
                return false;
            }

            if (!plantDef.CanEverPlantAt(cell, map))
            {
                return false;
            }

            if (!AnyColonistCanSow(cell, map, plantDef))
            {
                return false;
            }

            sowWork = plantDef.plant.sowWork;
            return sowWork > 0f;
        }

        private static bool CellHasPlantOfDef(IntVec3 cell, Map map, ThingDef plantDef)
        {
            Plant plant = cell.GetPlant(map);
            return plant != null && plant.def == plantDef;
        }

        private static bool PassesBlueprintAndFertilityChecks(IntVec3 cell, Map map)
        {
            foreach (Thing thing in cell.GetThingList(map))
            {
                if (thing is Blueprint or Frame)
                {
                    if (thing.Faction == Faction.OfPlayer)
                    {
                        Building edifice = cell.GetEdifice(map);
                        if (edifice == null || edifice.def.fertility < 0f)
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        private static bool PassesCavePlantRules(IntVec3 cell, Map map, ThingDef plantDef)
        {
            if (!plantDef.plant.cavePlant)
            {
                return true;
            }

            if (!cell.Roofed(map))
            {
                return false;
            }

            return map.glowGrid.GroundGlowAt(cell, ignoreCavePlants: true, ignoreSky: false) <= 0f;
        }

        private static bool PassesRoofRules(IntVec3 cell, Map map, ThingDef plantDef)
        {
            if (plantDef.plant.interferesWithRoof && cell.Roofed(map))
            {
                return false;
            }

            return true;
        }

        private static bool CellHasBlockingThing(IntVec3 cell, Map map)
        {
            foreach (Thing thing in cell.GetThingList(map))
            {
                if (BlocksPlanting(thing.def))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool AnyColonistCanSow(IntVec3 cell, Map map, ThingDef plantDef)
        {
            bool foundColonist = false;
            foreach (Pawn pawn in WorkMonitorUtility.MonitorColonists())
            {
                if (pawn.Map != map || !pawn.Spawned)
                {
                    continue;
                }

                foundColonist = true;

                if (cell.IsForbidden(pawn))
                {
                    continue;
                }

                if (plantDef.plant.sowMinSkill > 0
                    && pawn.skills != null
                    && pawn.skills.GetSkill(SkillDefOf.Plants).Level < plantDef.plant.sowMinSkill)
                {
                    continue;
                }

                if (pawn.CanReserve(cell, ignoreOtherReservations: false))
                {
                    return true;
                }
            }

            return !foundColonist;
        }

        private static bool BlocksPlanting(ThingDef def)
        {
            if (def == null)
            {
                return false;
            }

            return (def.building == null || !def.building.SupportsPlants)
                && (def.blockPlants || def.category == ThingCategory.Plant || def.Fillage > FillCategory.None || def.IsEdifice());
        }
    }
}
