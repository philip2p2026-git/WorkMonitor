using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;
using WorkMonitor.Tracking;

namespace WorkMonitor.Diagnostics
{
    public static class WorkMonitorPerfRecorder
    {
        private const string CsvHeader =
            "session_id,colony,map,hour_index,game_tick,realtime_sec,category,call_count,total_us,avg_us,counter_value,notes";

        private static readonly Dictionary<string, PerfAccumulator> accumulators = new Dictionary<string, PerfAccumulator>();
        private static readonly Dictionary<string, string> categoryNotes = new Dictionary<string, string>();

        private static int sessionId;
        private static int lastFlushedHour = -1;
        private static string sessionFilePath;
        private static bool headerWritten;

        public static bool Enabled =>
            WorkMonitorMod.Settings?.enablePerfLogging == true;

        public static void OnSettingsToggled(bool enabled)
        {
            if (enabled)
            {
                ResetSession();
            }
            else
            {
                Flush(force: true);
                ClearAccumulators();
            }
        }

        public static void ResetSession()
        {
            Flush(force: true);
            ClearAccumulators();
            sessionId = Find.TickManager?.TicksGame ?? 0;
            lastFlushedHour = -1;
            sessionFilePath = null;
            headerWritten = false;
        }

        public static void Record(string category, long elapsedTicks)
        {
            if (!Enabled || category.NullOrEmpty() || elapsedTicks < 0)
            {
                return;
            }

            long micros = TicksToMicroseconds(elapsedTicks);
            if (!accumulators.TryGetValue(category, out PerfAccumulator acc))
            {
                acc = new PerfAccumulator();
                accumulators[category] = acc;
            }

            acc.CallCount++;
            acc.TotalMicroseconds += micros;
        }

        public static void SetCategoryNote(string category, string notes)
        {
            if (!Enabled || category.NullOrEmpty())
            {
                return;
            }

            categoryNotes[category] = notes ?? string.Empty;
        }

        public static void TryFlushOnHourBoundary(int hourIndex)
        {
            if (!Enabled || hourIndex == lastFlushedHour)
            {
                return;
            }

            int flushEvery = Mathf.Max(1, WorkMonitorMod.Settings?.perfLogFlushHours ?? 1);
            if (lastFlushedHour >= 0 && hourIndex - lastFlushedHour < flushEvery)
            {
                return;
            }

            Flush(force: false, hourIndex: hourIndex);
            lastFlushedHour = hourIndex;
        }

        public static bool TryExportNow(out string path, out string error)
        {
            path = null;
            error = null;
            if (!Enabled)
            {
                error = "Performance logging is disabled.";
                return false;
            }

            Flush(force: true);
            if (sessionFilePath.NullOrEmpty())
            {
                error = "No performance data recorded yet.";
                return false;
            }

            path = sessionFilePath;
            return true;
        }

        public static void OpenPerfDirectory()
        {
            Directory.CreateDirectory(PerfDirectory);
            Application.OpenURL("file:///" + PerfDirectory.Replace('\\', '/'));
        }

        public static void FlushOnGameEnd()
        {
            if (!Enabled)
            {
                return;
            }

            Flush(force: true);
        }

        private static string PerfDirectory =>
            Path.Combine(GenFilePaths.SaveDataFolderPath, "WorkMonitor", "Perf");

        private static void Flush(bool force, int hourIndex = -1)
        {
            if (!Enabled && !force)
            {
                return;
            }

            if (accumulators.Count == 0 && !force)
            {
                return;
            }

            if (Current.Game == null)
            {
                return;
            }

            int gameTick = Find.TickManager.TicksGame;
            int hour = hourIndex >= 0 ? hourIndex : WorkMonitorUtility.CurrentHourIndex();
            float realtime = Time.realtimeSinceStartup;

            EnsureSessionFile();
            if (sessionFilePath.NullOrEmpty())
            {
                return;
            }

            var lines = new List<string>();
            if (!headerWritten)
            {
                lines.Add(CsvHeader);
                headerWritten = true;
            }

            string colony = ColonyLabel();
            string map = MapLabel();
            AppendTimingRows(lines, colony, map, hour, gameTick, realtime);
            AppendCounterRows(lines, colony, map, hour, gameTick, realtime);

            try
            {
                Directory.CreateDirectory(PerfDirectory);
                if (lines.Count > 0)
                {
                    File.AppendAllText(sessionFilePath, string.Join("\n", lines) + "\n", Encoding.UTF8);
                }
            }
            catch (IOException ex)
            {
                Log.Warning("[WorkMonitor] Perf log flush failed: " + ex.Message);
            }

            ClearAccumulators();
        }

        private static void AppendTimingRows(
            List<string> lines,
            string colony,
            string map,
            int hour,
            int gameTick,
            float realtime)
        {
            foreach (KeyValuePair<string, PerfAccumulator> entry in accumulators)
            {
                PerfAccumulator acc = entry.Value;
                if (acc.CallCount <= 0)
                {
                    continue;
                }

                long avg = acc.TotalMicroseconds / acc.CallCount;
                categoryNotes.TryGetValue(entry.Key, out string notes);
                lines.Add(FormatRow(
                    colony,
                    map,
                    hour,
                    gameTick,
                    realtime,
                    entry.Key,
                    acc.CallCount,
                    acc.TotalMicroseconds,
                    avg,
                    0,
                    notes));
            }
        }

        private static void AppendCounterRows(
            List<string> lines,
            string colony,
            string map,
            int hour,
            int gameTick,
            float realtime)
        {
            WorkActivityTracker tracker = WorkActivityTracker.Instance;
            MapWorkSampler sampler = MapWorkSampler.Instance;

            int colonistCount = 0;
            foreach (Pawn _ in WorkMonitorUtility.MonitorColonists())
            {
                colonistCount++;
            }

            AppendCounterLine(lines, colony, map, hour, gameTick, realtime, "active_jobs", tracker?.ActiveJobCount ?? 0);
            AppendCounterLine(lines, colony, map, hour, gameTick, realtime, "group_buffer_count", tracker?.GroupBufferCount ?? 0);
            AppendCounterLine(lines, colony, map, hour, gameTick, realtime, "pawn_wg_buffer_count", tracker?.PawnWgBufferCount ?? 0);
            AppendCounterLine(lines, colony, map, hour, gameTick, realtime, "map_history_count", sampler?.HistoryCount ?? 0);
            AppendCounterLine(lines, colony, map, hour, gameTick, realtime, "colonist_count", colonistCount);
            AppendCounterLine(
                lines,
                colony,
                map,
                hour,
                gameTick,
                realtime,
                "map_sample_interval_hours",
                MapWorkSampler.NormalizeInterval(WorkMonitorMod.Settings?.mapSampleIntervalHours ?? 1));
            AppendCounterLine(lines, colony, map, hour, gameTick, realtime, "ui_active", IsMonitorUiActive() ? 1 : 0);
        }

        private static void AppendCounterLine(
            List<string> lines,
            string colony,
            string map,
            int hour,
            int gameTick,
            float realtime,
            string category,
            long counterValue,
            string notes = null)
        {
            lines.Add(FormatRow(colony, map, hour, gameTick, realtime, category, 0, 0, 0, counterValue, notes));
        }

        private static string FormatRow(
            string colony,
            string map,
            int hour,
            int gameTick,
            float realtime,
            string category,
            long callCount,
            long totalUs,
            long avgUs,
            long counterValue,
            string notes)
        {
            return string.Join(",",
                Csv(sessionId.ToString()),
                Csv(colony),
                Csv(map),
                Csv(hour.ToString()),
                Csv(gameTick.ToString()),
                Csv(realtime.ToString("0.###")),
                Csv(category),
                Csv(callCount.ToString()),
                Csv(totalUs.ToString()),
                Csv(avgUs.ToString()),
                Csv(counterValue.ToString()),
                Csv(notes ?? string.Empty));
        }

        private static void EnsureSessionFile()
        {
            if (!sessionFilePath.NullOrEmpty())
            {
                return;
            }

            if (sessionId <= 0)
            {
                sessionId = Find.TickManager?.TicksGame ?? 0;
            }

            string safeColony = SanitizeFileName(ColonyLabel());
            string safeMap = SanitizeFileName(MapLabel());
            string fileName = safeColony + "_" + safeMap + "_" + sessionId + ".csv";
            sessionFilePath = Path.Combine(PerfDirectory, fileName);
        }

        private static void ClearAccumulators()
        {
            accumulators.Clear();
            categoryNotes.Clear();
        }

        private static long TicksToMicroseconds(long elapsedTicks)
        {
            if (elapsedTicks <= 0)
            {
                return 0;
            }

            return elapsedTicks * 1_000_000L / Stopwatch.Frequency;
        }

        private static bool IsMonitorUiActive()
        {
            return Find.WindowStack != null && Find.WindowStack.IsOpen<WorkGroupMonitorWindow>();
        }

        private static string ColonyLabel()
        {
            return Find.World?.info?.name ?? "Colony";
        }

        private static string MapLabel()
        {
            Map map = Find.CurrentMap ?? Find.AnyPlayerHomeMap;
            if (map == null)
            {
                return "Map";
            }

            return map.Parent?.LabelCap ?? "Map";
        }

        private static string SanitizeFileName(string value)
        {
            if (value.NullOrEmpty())
            {
                return "perf";
            }

            foreach (char c in Path.GetInvalidFileNameChars())
            {
                value = value.Replace(c, '_');
            }

            return value.Replace(' ', '_');
        }

        private static string Csv(string value)
        {
            value ??= string.Empty;
            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r"))
            {
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            }

            return value;
        }

        private class PerfAccumulator
        {
            public long CallCount;
            public long TotalMicroseconds;
        }
    }
}
