using System.IO;
using HarmonyLib;
using Verse;
using WorkMonitor.Tracking;

namespace WorkMonitor.Patches
{
    [HarmonyPatch(typeof(GameDataSaveLoader), nameof(GameDataSaveLoader.SaveGame))]
    public static class Patch_GameDataSaveLoader_SaveGame
    {
        public static void Prefix(string fileName)
        {
            WorkMonitorSaveTracker.SetFromPath(fileName);
        }

        public static void Postfix()
        {
            WorkMonitorSidecarStorage.Save();
        }
    }

    [HarmonyPatch(typeof(GameDataSaveLoader), nameof(GameDataSaveLoader.LoadGame), typeof(string))]
    public static class Patch_GameDataSaveLoader_LoadGame_String
    {
        public static void Prefix(string saveFileName)
        {
            WorkMonitorSaveTracker.SetFromPath(saveFileName);
        }

        public static void Postfix()
        {
            LongEventHandler.ExecuteWhenFinished(WorkMonitorSidecarStorage.TryLoadIntoTrackers);
        }
    }

    [HarmonyPatch(typeof(GameDataSaveLoader), nameof(GameDataSaveLoader.LoadGame), typeof(FileInfo))]
    public static class Patch_GameDataSaveLoader_LoadGame_FileInfo
    {
        public static void Prefix(FileInfo saveFile)
        {
            WorkMonitorSaveTracker.SetFromPath(saveFile?.FullName);
        }

        public static void Postfix()
        {
            LongEventHandler.ExecuteWhenFinished(WorkMonitorSidecarStorage.TryLoadIntoTrackers);
        }
    }

    [HarmonyPatch(typeof(Game), nameof(Game.ExposeData))]
    public static class Patch_Game_ExposeData_Sidecar
    {
        private static WorkActivityTracker savingTracker;
        private static MapWorkSampler savingSampler;

        public static void Prefix(Game __instance)
        {
            if (Scribe.mode != LoadSaveMode.Saving)
            {
                return;
            }

            savingTracker = __instance.GetComponent<WorkActivityTracker>();
            savingSampler = __instance.GetComponent<MapWorkSampler>();
            if (savingTracker != null)
            {
                __instance.components.Remove(savingTracker);
            }

            if (savingSampler != null)
            {
                __instance.components.Remove(savingSampler);
            }
        }

        public static void Postfix(Game __instance)
        {
            if (Scribe.mode != LoadSaveMode.Saving)
            {
                return;
            }

            if (savingTracker != null)
            {
                __instance.components.Add(savingTracker);
                savingTracker = null;
            }

            if (savingSampler != null)
            {
                __instance.components.Add(savingSampler);
                savingSampler = null;
            }
        }
    }
}
