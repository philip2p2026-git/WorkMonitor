using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using WorkMonitor.Diagnostics;
using WorkMonitor.Tracking;

namespace WorkMonitor.Patches
{
    [HarmonyPatch(typeof(JobDriver), nameof(JobDriver.DriverTick))]
    public static class Patch_JobDriverTick
    {
        public static void Postfix(JobDriver __instance)
        {
            if (__instance?.pawn == null || !__instance.pawn.IsColonist)
            {
                return;
            }

            using (new WorkMonitorPerfScope("job_driver_tick"))
            {
                int tick = Find.TickManager.TicksGame;
                WorkActivityTracker tracker = WorkActivityTracker.Instance;
                tracker?.SampleJobTick(__instance.pawn, tick);

                if (__instance is JobDriver_DoBill)
                {
                    tracker?.SampleBillWorkLeft(__instance.pawn, tick);
                }
            }
        }
    }
}
