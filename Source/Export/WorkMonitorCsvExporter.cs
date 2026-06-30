using System.Collections.Generic;
using System.IO;
using System.Text;
using RimWorld;
using Verse;
using WorkMonitor.Groups;
using WorkMonitor.Tracking;

namespace WorkMonitor.Export
{
    public static class WorkMonitorCsvExporter
    {
        private const string ColonistHeader =
            "colony,map,pawn_id,colonist_label,presence,work_giver,tier,period_id,period_start_hour,period_end_hour," +
            "job_count,endless_job_count,ticks,travel_ticks,work_ticks,work_units";

        private const string MapWorkGiverHeader =
            "colony,map,hour_index,sample_tick,game_datetime,work_giver,open_tasks,new_today_open_tasks,work_left,new_today_work_left";

        public static bool TryExportColonistRecords(out string path, out string error)
        {
            path = null;
            error = null;
            if (Current.Game == null)
            {
                error = "No active game.";
                return false;
            }

            WorkActivityTracker tracker = WorkActivityTracker.EnsureRegistered();
            if (tracker == null)
            {
                error = "Work activity tracker not available.";
                return false;
            }

            var lines = new List<string> { ColonistHeader };
            string colony = ColonyLabel();
            string map = MapLabel();

            foreach (KeyValuePair<int, Dictionary<string, WorkHistoryTierBuffer>> pawnEntry in tracker.EnumeratePawnWorkGiverHistory())
            {
                int pawnId = pawnEntry.Key;
                string label = ColonistWorkQuery.ResolveLabel(pawnId, tracker);
                string presence = ColonistWorkQuery.IsAbsent(pawnId, tracker) ? "absent" : "present";

                foreach (KeyValuePair<string, WorkHistoryTierBuffer> wgEntry in pawnEntry.Value)
                {
                    AppendColonistBufferRows(lines, colony, map, pawnId, label, presence, wgEntry.Key, wgEntry.Value);
                }
            }

            if (lines.Count <= 1)
            {
                error = "No colonist work records to export.";
                return false;
            }

            return TryWriteCsv("Colonists", lines, out path, out error);
        }

        public static bool TryExportMapWorkGiverRecords(out string path, out string error)
        {
            path = null;
            error = null;
            if (Current.Game == null)
            {
                error = "No active game.";
                return false;
            }

            MapWorkSampler sampler = MapWorkSampler.EnsureRegistered();
            if (sampler == null)
            {
                error = "Map work sampler not available.";
                return false;
            }

            IReadOnlyList<MapWorkSnapshot> history = sampler.GetHistory();
            if (history == null || history.Count == 0)
            {
                error = "No map workgiver snapshots to export.";
                return false;
            }

            var lines = new List<string> { MapWorkGiverHeader };
            string colony = ColonyLabel();
            string map = MapLabel();

            foreach (MapWorkSnapshot snapshot in history)
            {
                if (snapshot?.perWorkGiver == null)
                {
                    continue;
                }

                string gameDate = WorkMonitorUtility.FormatGameDateTime(snapshot.sampleTick);
                foreach (MapWorkGiverSnapshot wg in snapshot.perWorkGiver.Values)
                {
                    if (wg == null || wg.workGiverDefName.NullOrEmpty())
                    {
                        continue;
                    }

                    lines.Add(string.Join(",",
                        Csv(colony),
                        Csv(map),
                        Csv(snapshot.hourIndex.ToString()),
                        Csv(snapshot.sampleTick.ToString()),
                        Csv(gameDate),
                        Csv(wg.workGiverDefName),
                        Csv(wg.openTaskCount.ToString()),
                        Csv(wg.newTodayOpenTaskCount.ToString()),
                        Csv(wg.workLeftTotal.ToString("0.##")),
                        Csv(wg.newTodayWorkLeftTotal.ToString("0.##"))));
                }
            }

            if (lines.Count <= 1)
            {
                error = "No map workgiver snapshots to export.";
                return false;
            }

            return TryWriteCsv("MapWorkGivers", lines, out path, out error);
        }

        private static void AppendColonistBufferRows(
            List<string> lines,
            string colony,
            string map,
            int pawnId,
            string label,
            string presence,
            string workGiver,
            WorkHistoryTierBuffer buffer)
        {
            if (buffer == null)
            {
                return;
            }

            foreach (HourlyWorkBucket bucket in buffer.Buckets)
            {
                if (!HasPawnWork(bucket, pawnId))
                {
                    continue;
                }

                lines.Add(FormatColonistRow(colony, map, pawnId, label, presence, workGiver, "hourly",
                    bucket.hourIndex.ToString(), bucket.hourIndex, bucket.hourIndex + 1, bucket, pawnId));
            }

            foreach (DailyWorkBucket bucket in buffer.DailyBuckets)
            {
                if (!HasPawnWork(bucket.pawnFields, pawnId))
                {
                    continue;
                }

                lines.Add(FormatColonistRow(colony, map, pawnId, label, presence, workGiver, "daily",
                    bucket.dayId.ToString(), bucket.startHourIndex, bucket.endHourIndex, bucket.pawnFields, pawnId));
            }

            foreach (QuadrumWorkBucket bucket in buffer.QuadrumBuckets)
            {
                if (!HasPawnWork(bucket.pawnFields, pawnId))
                {
                    continue;
                }

                lines.Add(FormatColonistRow(colony, map, pawnId, label, presence, workGiver, "quadrum",
                    bucket.quadrumKey.ToString(), 0, 0, bucket.pawnFields, pawnId));
            }

            foreach (YearWorkBucket bucket in buffer.YearBuckets)
            {
                if (!HasPawnWork(bucket.pawnFields, pawnId))
                {
                    continue;
                }

                lines.Add(FormatColonistRow(colony, map, pawnId, label, presence, workGiver, "year",
                    bucket.year.ToString(), 0, 0, bucket.pawnFields, pawnId));
            }
        }

        private static string FormatColonistRow(
            string colony,
            string map,
            int pawnId,
            string label,
            string presence,
            string workGiver,
            string tier,
            string periodId,
            int periodStartHour,
            int periodEndHour,
            HourlyWorkBucket bucket,
            int pawnIdStats)
        {
            return string.Join(",",
                Csv(colony),
                Csv(map),
                Csv(pawnId.ToString()),
                Csv(label),
                Csv(presence),
                Csv(workGiver),
                Csv(tier),
                Csv(periodId),
                Csv(periodStartHour.ToString()),
                Csv(periodEndHour.ToString()),
                Csv(PawnBucketMergeUtility.GetInt(bucket.pawnJobCount, pawnIdStats).ToString()),
                Csv(PawnBucketMergeUtility.GetInt(bucket.pawnEndlessJobCount, pawnIdStats).ToString()),
                Csv(PawnBucketMergeUtility.GetInt(bucket.pawnTicksSpent, pawnIdStats).ToString()),
                Csv(PawnBucketMergeUtility.GetInt(bucket.pawnTravelTicksSpent, pawnIdStats).ToString()),
                Csv(PawnBucketMergeUtility.GetInt(bucket.pawnWorkTicksSpent, pawnIdStats).ToString()),
                Csv(PawnBucketMergeUtility.GetFloat(bucket.pawnWorkUnitsSpent, pawnIdStats).ToString("0.##")));
        }

        private static string FormatColonistRow(
            string colony,
            string map,
            int pawnId,
            string label,
            string presence,
            string workGiver,
            string tier,
            string periodId,
            int periodStartHour,
            int periodEndHour,
            PawnWorkBucketFields fields,
            int pawnIdStats)
        {
            return string.Join(",",
                Csv(colony),
                Csv(map),
                Csv(pawnId.ToString()),
                Csv(label),
                Csv(presence),
                Csv(workGiver),
                Csv(tier),
                Csv(periodId),
                Csv(periodStartHour.ToString()),
                Csv(periodEndHour.ToString()),
                Csv(PawnBucketMergeUtility.GetInt(fields.pawnJobCount, pawnIdStats).ToString()),
                Csv(PawnBucketMergeUtility.GetInt(fields.pawnEndlessJobCount, pawnIdStats).ToString()),
                Csv(PawnBucketMergeUtility.GetInt(fields.pawnTicksSpent, pawnIdStats).ToString()),
                Csv(PawnBucketMergeUtility.GetInt(fields.pawnTravelTicksSpent, pawnIdStats).ToString()),
                Csv(PawnBucketMergeUtility.GetInt(fields.pawnWorkTicksSpent, pawnIdStats).ToString()),
                Csv(PawnBucketMergeUtility.GetFloat(fields.pawnWorkUnitsSpent, pawnIdStats).ToString("0.##")));
        }

        private static bool HasPawnWork(HourlyWorkBucket bucket, int pawnId)
        {
            return PawnBucketMergeUtility.GetInt(bucket.pawnJobCount, pawnId) > 0
                || PawnBucketMergeUtility.GetInt(bucket.pawnEndlessJobCount, pawnId) > 0
                || PawnBucketMergeUtility.GetInt(bucket.pawnTicksSpent, pawnId) > 0
                || PawnBucketMergeUtility.GetFloat(bucket.pawnWorkUnitsSpent, pawnId) > 0f;
        }

        private static bool HasPawnWork(PawnWorkBucketFields fields, int pawnId)
        {
            if (fields == null)
            {
                return false;
            }

            return PawnBucketMergeUtility.GetInt(fields.pawnJobCount, pawnId) > 0
                || PawnBucketMergeUtility.GetInt(fields.pawnEndlessJobCount, pawnId) > 0
                || PawnBucketMergeUtility.GetInt(fields.pawnTicksSpent, pawnId) > 0
                || PawnBucketMergeUtility.GetFloat(fields.pawnWorkUnitsSpent, pawnId) > 0f;
        }

        private static bool TryWriteCsv(string prefix, List<string> lines, out string path, out string error)
        {
            path = null;
            error = null;
            try
            {
                string dir = Path.Combine(GenFilePaths.SaveDataFolderPath, "WorkMonitor", "Exports");
                Directory.CreateDirectory(dir);
                string safeColony = SanitizeFileName(ColonyLabel());
                string timestamp = GenDate.DateFullStringAt(Find.TickManager.TicksAbs, WorkMonitorUtility.MapLongitude());
                timestamp = SanitizeFileName(timestamp);
                string fileName = prefix + "_" + safeColony + "_" + timestamp + ".csv";
                path = Path.Combine(dir, fileName);
                File.WriteAllText(path, string.Join("\n", lines), Encoding.UTF8);
                return true;
            }
            catch (IOException ex)
            {
                error = ex.Message;
                return false;
            }
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
                return "export";
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
    }
}
