using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using WorkMonitor.Diagnostics;
using WorkMonitor.Tracking;

namespace WorkMonitor.Patches
{
    [HarmonyPatch(typeof(Pawn_JobTracker), nameof(Pawn_JobTracker.StartJob))]
    public static class Patch_RecordWorkStart
    {
        public static void Prefix(Pawn_JobTracker __instance)
        {
            using (new WorkMonitorPerfScope("job_start_end"))
            {
                Pawn pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();
                if (pawn == null)
                {
                    return;
                }

                WorkActivityTracker tracker = WorkActivityTracker.EnsureRegistered();
                tracker?.RecordJobEnd(pawn, null, __instance.curJob, Find.TickManager.TicksGame);
            }
        }

        public static void Postfix(Pawn_JobTracker __instance, Job newJob)
        {
            using (new WorkMonitorPerfScope("job_start_end"))
            {
                Pawn pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();
                if (newJob?.workGiverDef == null || pawn == null)
                {
                    return;
                }

                WorkActivityTracker tracker = WorkActivityTracker.EnsureRegistered();
                tracker?.RecordJobStart(pawn, newJob.workGiverDef, newJob, Find.TickManager.TicksGame);
            }
        }
    }

    [HarmonyPatch(typeof(Pawn_JobTracker), nameof(Pawn_JobTracker.EndCurrentJob))]
    public static class Patch_RecordWorkEnd
    {
        public static void Prefix(Pawn_JobTracker __instance)
        {
            using (new WorkMonitorPerfScope("job_start_end"))
            {
                Pawn pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();
                if (pawn == null)
                {
                    return;
                }

                Job endingJob = __instance.curJob;
                WorkGiverDef workGiver = endingJob?.workGiverDef;
                WorkActivityTracker tracker = WorkActivityTracker.EnsureRegistered();
                tracker?.RecordJobEnd(pawn, workGiver, endingJob, Find.TickManager.TicksGame);
            }
        }
    }
}
