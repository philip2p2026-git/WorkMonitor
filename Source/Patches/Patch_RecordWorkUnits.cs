using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using WorkMonitor.Tracking;

namespace WorkMonitor.Patches
{
    [HarmonyPatch(typeof(JobDriver), "DriverTickInterval")]
    public static class Patch_RecordWorkUnits
    {
        public static void Postfix(JobDriver __instance, int delta)
        {
            Pawn pawn = __instance.pawn;
            Job job = __instance.job;
            if (pawn == null || job == null || job.workGiverDef == null || !pawn.IsColonistPlayerControlled)
            {
                return;
            }

            if (pawn.CurJob != job || delta <= 0)
            {
                return;
            }

            if (pawn.stances == null || !pawn.stances.FullBodyBusy)
            {
                return;
            }

            float workUnits = WorkMonitorUtility.EstimateWorkUnitsForInterval(pawn, job, delta);
            if (workUnits <= 0f)
            {
                return;
            }

            WorkActivityTracker tracker = WorkActivityTracker.EnsureRegistered();
            tracker?.RecordWorkUnits(pawn, job.workGiverDef, workUnits, Find.TickManager.TicksGame);
        }
    }
}
