using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace WorkMonitor.Tracking
{
    public static class WorkUnitEstimator
    {
        private const float MinWorkSpeed = 0.05f;

        public static bool TryEstimateWorkUnits(Pawn pawn, WorkGiverDef workGiver, Job job, int workTicks, out float units)
        {
            units = 0f;
            if (pawn == null || workGiver == null || workTicks <= 0)
            {
                return false;
            }

            StatDef stat = ResolveWorkSpeedStat(workGiver);
            if (stat == null)
            {
                return false;
            }

            float speed = Mathf.Max(MinWorkSpeed, pawn.GetStatValue(stat));
            units = workTicks * speed;
            return units > 0f;
        }

        private static StatDef ResolveWorkSpeedStat(WorkGiverDef workGiver)
        {
            if (workGiver == null)
            {
                return StatDefOf.WorkSpeedGlobal;
            }

            if (EndlessWorkGiverUtility.IsEndless(workGiver))
            {
                if (workGiver.defName == "Research")
                {
                    return StatDefOf.ResearchSpeed;
                }

                if (workGiver.defName == "Drill")
                {
                    return StatDefOf.MiningSpeed;
                }

                return StatDefOf.WorkSpeedGlobal;
            }

            WorkTypeDef workType = workGiver.workType;
            if (workType != null)
            {
                if (workType == WorkTypeDefOf.Mining)
                {
                    return StatDefOf.MiningSpeed;
                }

                if (workType == WorkTypeDefOf.Construction)
                {
                    return StatDefOf.ConstructionSpeed;
                }

                if (workType == WorkTypeDefOf.Growing)
                {
                    return StatDefOf.PlantWorkSpeed;
                }

                if (workType == WorkTypeDefOf.Doctor)
                {
                    return StatDefOf.MedicalTendSpeed;
                }
            }

            return StatDefOf.WorkSpeedGlobal;
        }
    }
}
