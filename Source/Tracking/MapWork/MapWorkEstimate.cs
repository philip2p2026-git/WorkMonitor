using RimWorld;
using UnityEngine;
using Verse;

namespace WorkMonitor.Tracking.MapWork
{
    public static class MapWorkEstimate
    {
        public static float At100Percent(float workAmount, WorkTypeDef workType)
        {
            if (workAmount <= 0f)
            {
                return 0f;
            }

            return workAmount;
        }

        public static float FromRecipe(RecipeDef recipe, int count = 1)
        {
            if (recipe == null || count <= 0)
            {
                return 0f;
            }

            return Mathf.Max(0f, recipe.WorkAmountTotal(null) * count);
        }

        public static float FromFrame(Frame frame)
        {
            return frame != null ? Mathf.Max(0f, frame.WorkLeft) : 0f;
        }

        public static float FromMineable(Mineable mineable)
        {
            return mineable != null ? Mathf.Max(0f, mineable.HitPoints) : 0f;
        }

        public static float FromFilth(Filth filth)
        {
            if (filth?.def?.filth == null)
            {
                return 0f;
            }

            return Mathf.Max(0f, filth.def.filth.cleaningWorkToReduceThickness * filth.thickness);
        }

        public static float FromThingDeconstruct(Thing thing)
        {
            if (thing?.def == null)
            {
                return 0f;
            }

            float workToBuild = thing.def.GetStatValueAbstract(StatDefOf.WorkToBuild, thing.Stuff);
            return Mathf.Max(0f, workToBuild);
        }

        public static float FromPlantCut(Plant plant)
        {
            if (plant?.def?.plant == null)
            {
                return 0f;
            }

            return Mathf.Max(0f, plant.def.plant.harvestWork);
        }

        public static float FromSmoothCell()
        {
            return 1500f;
        }

        public static float ResearchRemaining()
        {
            ResearchProjectDef project = Find.ResearchManager?.GetProject();
            if (project == null)
            {
                return 0f;
            }

            float remaining = project.baseCost - Find.ResearchManager.GetProgress(project);
            return Mathf.Max(0f, remaining);
        }

        public static float FromRepair(Thing thing)
        {
            if (thing == null || thing.Destroyed)
            {
                return 0f;
            }

            int missing = thing.MaxHitPoints - thing.HitPoints;
            if (missing <= 0)
            {
                return 0f;
            }

            float workPerHp = thing.def.GetStatValueAbstract(StatDefOf.WorkToBuild, thing.Stuff);
            if (workPerHp <= 0f)
            {
                workPerHp = 100f;
            }

            return missing * workPerHp / Mathf.Max(1f, thing.MaxHitPoints);
        }
    }
}
