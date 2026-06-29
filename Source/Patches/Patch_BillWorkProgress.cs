using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using WorkMonitor.Tracking;

namespace WorkMonitor.Patches
{
    [HarmonyPatch(typeof(JobDriver), nameof(JobDriver.DriverTick))]
    public static class Patch_BillWorkProgress
    {
        public static void Postfix(JobDriver __instance)
        {
            if (__instance?.pawn == null || !__instance.pawn.IsColonist)
            {
                return;
            }

            if (__instance is not JobDriver_DoBill)
            {
                return;
            }

            WorkActivityTracker.Instance?.SampleBillWorkLeft(__instance.pawn, Find.TickManager.TicksGame);
        }
    }
}
