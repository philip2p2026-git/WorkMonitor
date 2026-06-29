using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace WorkMonitor.Tracking.MapWork.Providers
{
    public class DesignationMapWorkProvider : IMapWorkTargetProvider
    {
        private struct DesignationRule
        {
            public readonly string DesignationDefName;
            public readonly string WorkGiverDefName;
            public readonly Func<Designation, Map, float> EstimateWork;

            public DesignationRule(string designationDefName, string workGiverDefName, Func<Designation, Map, float> estimateWork)
            {
                DesignationDefName = designationDefName;
                WorkGiverDefName = workGiverDefName;
                EstimateWork = estimateWork ?? ((_, __) => 0f);
            }
        }

        private static readonly DesignationRule[] Rules =
        {
            new DesignationRule("CutPlant", "PlantsCut", EstimatePlant),
            new DesignationRule("HarvestPlant", "GrowerHarvest", EstimatePlant),
            new DesignationRule("ExtractTree", "ExtractTree", EstimatePlant),
            new DesignationRule("Deconstruct", "Deconstruct", EstimateThingDeconstruct),
            new DesignationRule("Uninstall", "Uninstall", EstimateThingDeconstruct),
            new DesignationRule("Hunt", "HunterHunt", (_, __) => 0f),
            new DesignationRule("SmoothFloor", "ConstructSmoothFloors", (_, __) => MapWorkEstimate.FromSmoothCell()),
            new DesignationRule("SmoothWall", "ConstructSmoothWalls", (_, __) => MapWorkEstimate.FromSmoothCell()),
            new DesignationRule("RemoveFloor", "ConstructRemoveFloors", (_, __) => MapWorkEstimate.FromSmoothCell()),
            new DesignationRule("RemoveFoundation", "ConstructRemoveFoundations", (_, __) => MapWorkEstimate.FromSmoothCell()),
            new DesignationRule("PaintBuilding", "PaintBuilding", (_, __) => MapWorkEstimate.FromSmoothCell()),
            new DesignationRule("PaintFloor", "PaintFloor", (_, __) => MapWorkEstimate.FromSmoothCell()),
            new DesignationRule("RemovePaintBuilding", "RemovePaintBuilding", (_, __) => MapWorkEstimate.FromSmoothCell()),
            new DesignationRule("RemovePaintFloor", "RemovePaintFloor", (_, __) => MapWorkEstimate.FromSmoothCell()),
            new DesignationRule("FillIn", "FillIn", EstimateThingDeconstruct),
            new DesignationRule("Strip", "Strip", (_, __) => 0f),
            new DesignationRule("Slaughter", "Slaughter", (_, __) => 0f),
            new DesignationRule("Tame", "Tame", (_, __) => 0f),
            new DesignationRule("ReleaseAnimalToWild", "ReleaseToWild", (_, __) => 0f),
            new DesignationRule("Flick", "Flick", (_, __) => 0f),
            new DesignationRule("Open", "Open", (_, __) => 0f),
            new DesignationRule("EjectFuel", "EjectFuel", (_, __) => 0f),
            new DesignationRule("ExtractSkull", "ExtractSkull", (_, __) => 0f),
        };

        public void Collect(Map map, List<ScannedMapTarget> targets)
        {
            foreach (DesignationRule rule in Rules)
            {
                DesignationDef designationDef = DefDatabase<DesignationDef>.GetNamedSilentFail(rule.DesignationDefName);
                WorkGiverDef workGiver = MapWorkAttribution.GetWorkGiver(rule.WorkGiverDefName);
                if (designationDef == null || workGiver == null)
                {
                    continue;
                }

                List<WorkGiverDef> workGivers = new List<WorkGiverDef> { workGiver };

                foreach (Designation designation in map.designationManager.SpawnedDesignationsOfDef(designationDef))
                {
                    if (!designation.target.IsValid)
                    {
                        continue;
                    }

                    float work = rule.EstimateWork(designation, map);
                    string key = "designation:" + rule.DesignationDefName + ":" + DesignationTargetKey(designation);
                    targets.Add(new ScannedMapTarget(key, work, workGivers));
                }
            }
        }

        private static string DesignationTargetKey(Designation designation)
        {
            if (designation.target.HasThing)
            {
                return "thing:" + designation.target.Thing.thingIDNumber;
            }

            return "cell:" + designation.target.Cell;
        }

        private static float EstimatePlant(Designation designation, Map map)
        {
            if (designation.target.HasThing && designation.target.Thing is Plant plant)
            {
                return MapWorkEstimate.FromPlantCut(plant);
            }

            return 0f;
        }

        private static float EstimateThingDeconstruct(Designation designation, Map map)
        {
            if (designation.target.HasThing)
            {
                return MapWorkEstimate.FromThingDeconstruct(designation.target.Thing);
            }

            return 0f;
        }
    }
}
