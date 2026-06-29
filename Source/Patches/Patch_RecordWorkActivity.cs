using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using WorkMonitor.Tracking;

namespace WorkMonitor.Patches
{
    [HarmonyPatch(typeof(Pawn_JobTracker), nameof(Pawn_JobTracker.StartJob))]
    public static class Patch_RecordWorkStart
    {
        public static void Prefix(Pawn_JobTracker __instance)
        {
            Pawn pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();
            if (pawn == null)
            {
                return;
            }

            WorkActivityTracker tracker = WorkActivityTracker.EnsureRegistered();
            tracker?.RecordJobEnd(pawn, null, Find.TickManager.TicksGame);
        }

        public static void Postfix(Pawn_JobTracker __instance, Job newJob)
        {
            Pawn pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();
            if (newJob?.workGiverDef == null || pawn == null)
            {
                return;
            }

            WorkActivityTracker tracker = WorkActivityTracker.EnsureRegistered();
            tracker?.RecordJobStart(pawn, newJob.workGiverDef, Find.TickManager.TicksGame);
        }
    }

    [HarmonyPatch(typeof(Pawn_JobTracker), nameof(Pawn_JobTracker.EndCurrentJob))]
    public static class Patch_RecordWorkEnd
    {
        public static void Postfix(Pawn_JobTracker __instance)
        {
            Pawn pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();
            if (pawn == null)
            {
                return;
            }

            WorkGiverDef workGiver = __instance.curJob?.workGiverDef;
            WorkActivityTracker tracker = WorkActivityTracker.EnsureRegistered();
            tracker?.RecordJobEnd(pawn, workGiver, Find.TickManager.TicksGame);
        }
    }
}
