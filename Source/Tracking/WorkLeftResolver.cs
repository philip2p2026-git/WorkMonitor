using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace WorkMonitor.Tracking
{
    public static class WorkLeftResolver
    {
        private const float InvalidWorkLeft = -5000f;

        public static bool TryGetWorkLeft(Job job, Pawn pawn, out float workLeft)
        {
            workLeft = 0f;
            if (job == null)
            {
                return false;
            }

            JobDriver driver = pawn?.jobs?.curDriver;
            if (driver != null && driver.job == job)
            {
                if (TryGetBillDriverWorkLeft(pawn, out workLeft))
                {
                    return workLeft > 0f;
                }

                var field = AccessTools.Field(driver.GetType(), "workLeft");
                if (field != null && field.FieldType == typeof(float))
                {
                    float value = (float)field.GetValue(driver);
                    if (value > InvalidWorkLeft)
                    {
                        workLeft = value;
                        return workLeft > 0f;
                    }
                }
            }

            Thing target = job.GetTarget(TargetIndex.A).Thing;
            if (TryGetThingWorkLeft(target, out workLeft))
            {
                return true;
            }

            target = job.GetTarget(TargetIndex.B).Thing;
            return TryGetThingWorkLeft(target, out workLeft);
        }

        public static bool TryGetBillDriverWorkLeft(Pawn pawn, out float workLeft)
        {
            workLeft = 0f;
            if (pawn?.jobs?.curDriver is JobDriver_DoBill billDriver && billDriver.workLeft > InvalidWorkLeft)
            {
                workLeft = billDriver.workLeft;
                return true;
            }

            return false;
        }

        public static bool TryGetThingWorkLeft(Thing thing, out float workLeft)
        {
            workLeft = 0f;
            if (thing == null)
            {
                return false;
            }

            if (thing is Frame frame)
            {
                workLeft = frame.WorkLeft;
                return workLeft > 0f;
            }

            if (thing is UnfinishedThing unfinished && unfinished.workLeft > InvalidWorkLeft)
            {
                workLeft = unfinished.workLeft;
                return workLeft > 0f;
            }

            if (thing is Mineable mineable)
            {
                workLeft = mineable.HitPoints;
                return workLeft > 0f;
            }

            return false;
        }

        public static bool TryGetBillBacklog(Bill bill, out float workLeft, out bool countable)
        {
            workLeft = 0f;
            countable = false;
            if (bill == null || bill.deleted || bill.suspended || bill.recipe == null)
            {
                return false;
            }

            if (bill is Bill_ProductionWithUft billWithUft && billWithUft.BoundUft != null
                && billWithUft.BoundUft.workLeft > InvalidWorkLeft)
            {
                workLeft += Mathf.Max(0f, billWithUft.BoundUft.workLeft);
            }

            if (bill is Bill_Production production)
            {
                float unitWork = bill.GetWorkAmount();
                if (production.repeatMode == BillRepeatModeDefOf.Forever)
                {
                    countable = true;
                    workLeft += Mathf.Max(unitWork, 0f);
                    return true;
                }

                if (production.repeatMode == BillRepeatModeDefOf.TargetCount)
                {
                    int made = bill.recipe.WorkerCounter.CountProducts(production);
                    int remaining = Mathf.Max(0, production.targetCount - made);
                    if (remaining > 0)
                    {
                        countable = true;
                        workLeft += remaining * unitWork;
                    }
                }
                else if (production.repeatMode == BillRepeatModeDefOf.RepeatCount)
                {
                    if (production.ShouldDoNow())
                    {
                        countable = true;
                        workLeft += unitWork;
                    }
                }
            }

            if (workLeft > 0f)
            {
                countable = true;
                return true;
            }

            return countable;
        }
    }
}
