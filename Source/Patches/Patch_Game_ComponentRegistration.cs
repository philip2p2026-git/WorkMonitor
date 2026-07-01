using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using WorkMonitor.Diagnostics;
using WorkMonitor.Tracking;

namespace WorkMonitor.Patches
{
    [HarmonyPatch(typeof(Game))]
    public static class Patch_Game_Constructor
    {
        public static IEnumerable<MethodBase> TargetMethods()
        {
            foreach (ConstructorInfo ctor in AccessTools.GetDeclaredConstructors(typeof(Game)))
            {
                yield return ctor;
            }
        }

        public static void Postfix(Game __instance)
        {
            __instance.components.RemoveAll(c => c is WorkActivityTracker || c is MapWorkSampler);
            WorkActivityTracker.ClearInstance();
            MapWorkSampler.ClearInstance();
        }
    }

    [HarmonyPatch(typeof(Game), nameof(Game.FinalizeInit))]
    public static class Patch_Game_FinalizeInit
    {
        public static void Postfix()
        {
            WorkActivityTracker.EnsureRegistered();
            MapWorkSampler sampler = MapWorkSampler.EnsureRegistered();
            sampler?.TrySampleIfDue(force: true);
        }
    }

    [HarmonyPatch(typeof(ScribeSaver), "InitSaving")]
    public static class Patch_ScribeSaver_InitSaving
    {
        public static void Prefix()
        {
            WorkMonitorPerfRecorder.FlushOnGameEnd();
        }
    }

    [HarmonyPatch(typeof(GenScene), nameof(GenScene.GoToMainMenu))]
    public static class Patch_GenScene_GoToMainMenu
    {
        public static void Prefix()
        {
            WorkMonitorPerfRecorder.FlushOnGameEnd();
        }
    }
}
