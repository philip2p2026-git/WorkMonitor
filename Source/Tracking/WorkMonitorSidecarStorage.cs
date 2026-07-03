using System.Collections.Generic;
using System.IO;
using Verse;

namespace WorkMonitor.Tracking
{
    public static class WorkMonitorSidecarStorage
    {
        private static string StorageDirectory => Path.Combine(GenFilePaths.SaveDataFolderPath, "WorkMonitor");

        public static void Save()
        {
            string saveName = WorkMonitorSaveTracker.CurrentSaveName;
            if (saveName.NullOrEmpty())
            {
                return;
            }

            var data = new WorkMonitorSidecarData();
            WorkActivityTracker tracker = WorkActivityTracker.Instance;
            MapWorkSampler sampler = MapWorkSampler.Instance;
            tracker?.WriteToSidecarData(data);
            sampler?.WriteToSidecarData(data);

            if (!data.HasAnyData())
            {
                DeleteForSave(saveName);
                return;
            }

            Directory.CreateDirectory(StorageDirectory);

            string path = GetPath(saveName);
            Scribe.saver.InitSaving(path, "WorkMonitorSidecar");
            try
            {
                Scribe_Deep.Look(ref data, "data");
            }
            finally
            {
                Scribe.saver.FinalizeSaving();
            }
        }

        public static void TryLoadIntoTrackers()
        {
            string saveName = WorkMonitorSaveTracker.CurrentSaveName;
            if (saveName.NullOrEmpty())
            {
                return;
            }

            WorkActivityTracker tracker = WorkActivityTracker.EnsureRegistered();
            MapWorkSampler sampler = MapWorkSampler.EnsureRegistered();
            if (tracker == null && sampler == null)
            {
                return;
            }

            bool trackerHasEmbedded = tracker != null && tracker.HasPersistedData();
            bool samplerHasEmbedded = sampler != null && sampler.HasPersistedData();
            if (trackerHasEmbedded && samplerHasEmbedded)
            {
                return;
            }

            string path = GetPath(saveName);
            if (!File.Exists(path))
            {
                return;
            }

            WorkMonitorSidecarData data = null;
            Scribe.loader.InitLoading(path);
            try
            {
                Scribe_Deep.Look(ref data, "data");
            }
            finally
            {
                Scribe.loader.FinalizeLoading();
            }

            if (data == null || !data.HasAnyData())
            {
                return;
            }

            if (tracker != null && !trackerHasEmbedded)
            {
                tracker.LoadFromSidecarData(data);
            }

            if (sampler != null && !samplerHasEmbedded)
            {
                sampler.LoadFromSidecarData(data);
            }
        }

        public static void DeleteForSave(string saveName)
        {
            if (saveName.NullOrEmpty())
            {
                return;
            }

            string path = GetPath(saveName);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        private static string GetPath(string saveName)
        {
            string safeName = saveName;
            foreach (char invalidChar in Path.GetInvalidFileNameChars())
            {
                safeName = safeName.Replace(invalidChar, '_');
            }

            return Path.Combine(StorageDirectory, safeName + ".xml");
        }
    }
}
