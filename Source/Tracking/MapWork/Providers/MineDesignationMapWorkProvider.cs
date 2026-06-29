using System.Collections.Generic;
using RimWorld;
using Verse;

namespace WorkMonitor.Tracking.MapWork.Providers
{
    public class MineDesignationMapWorkProvider : IMapWorkTargetProvider
    {
        public void Collect(Map map, List<ScannedMapTarget> targets)
        {
            DesignationDef mineDef = DesignationDefOf.Mine;
            if (mineDef == null)
            {
                return;
            }

            WorkGiverDef workGiver = MapWorkAttribution.MineWorkGiver();
            if (workGiver == null)
            {
                return;
            }

            List<WorkGiverDef> workGivers = new List<WorkGiverDef> { workGiver };

            foreach (Designation designation in map.designationManager.SpawnedDesignationsOfDef(mineDef))
            {
                if (designation.target.Thing is not Mineable mineable || !mineable.Spawned)
                {
                    continue;
                }

                if (!WorkLeftResolver.TryGetThingWorkLeft(mineable, out float workLeft))
                {
                    continue;
                }

                targets.Add(new ScannedMapTarget(
                    "mine:" + mineable.thingIDNumber,
                    workLeft,
                    workGivers));
            }

            DesignationDef mineVeinDef = DefDatabase<DesignationDef>.GetNamedSilentFail("MineVein");
            if (mineVeinDef == null)
            {
                return;
            }

            foreach (Designation designation in map.designationManager.SpawnedDesignationsOfDef(mineVeinDef))
            {
                if (designation.target.Thing is not Mineable mineable || !mineable.Spawned)
                {
                    continue;
                }

                if (!WorkLeftResolver.TryGetThingWorkLeft(mineable, out float workLeft))
                {
                    continue;
                }

                targets.Add(new ScannedMapTarget(
                    "minevein:" + mineable.thingIDNumber,
                    workLeft,
                    workGivers));
            }
        }
    }
}
