using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using WorkMonitor.Groups;

namespace WorkMonitor.Tracking
{
    public enum WorkLeftTrackingMode
    {
        Snapshot,
        BillIncremental
    }

    public class ActiveWorkJob
    {
        public string workGiverDefName;
        public int startTick;
        public int travelTicks;
        public int workTicks;
        public bool tracksWorkLeft;
        public float startWorkLeft;
        public WorkLeftTrackingMode trackingMode;
        public float lastBillWorkLeft = -1f;
        public float accumulatedWorkUnits;
    }

    public class WorkActivityTracker : GameComponent
    {
        private static WorkActivityTracker instance;

        private Dictionary<int, Dictionary<string, WorkActivityRecord>> pawnRecords =
            new Dictionary<int, Dictionary<string, WorkActivityRecord>>();

        private Dictionary<int, Dictionary<string, WorkHistoryTierBuffer>> pawnWorkGiverHistory =
            new Dictionary<int, Dictionary<string, WorkHistoryTierBuffer>>();

        private Dictionary<int, ActiveWorkJob> activeJobs = new Dictionary<int, ActiveWorkJob>();

        private Dictionary<string, WorkHistoryTierBuffer> groupHistory =
            new Dictionary<string, WorkHistoryTierBuffer>();

        private int lastPrunedHourIndex = -1;

        public static WorkActivityTracker Instance
        {
            get
            {
                if (instance == null && Current.Game != null)
                {
                    instance = Current.Game.GetComponent<WorkActivityTracker>();
                }

                return instance;
            }
        }

        public static void ClearInstance()
        {
            instance = null;
        }

        public static WorkActivityTracker EnsureRegistered()
        {
            if (Current.Game == null)
            {
                return null;
            }

            WorkActivityTracker tracker = Current.Game.GetComponent<WorkActivityTracker>();
            if (tracker == null)
            {
                tracker = new WorkActivityTracker(Current.Game);
                Current.Game.components.Add(tracker);
            }

            instance = tracker;
            return tracker;
        }

        public WorkActivityTracker(Game game)
        {
            instance = this;
        }

        public override void LoadedGame()
        {
            base.LoadedGame();
            PruneStaleData();
        }

        public override void GameComponentTick()
        {
            int hour = WorkMonitorUtility.CurrentHourIndex();
            if (hour != lastPrunedHourIndex)
            {
                lastPrunedHourIndex = hour;
                PruneStaleData();
            }
        }

        public void RecordJobStart(Pawn pawn, WorkGiverDef workGiver, Job job, int tick)
        {
            if (pawn == null || workGiver == null || !pawn.IsColonist)
            {
                return;
            }

            FinalizeActiveJob(pawn, tick, null, null);

            WorkActivityRecord record = GetOrCreateRecord(pawn.thingIDNumber, workGiver.defName);
            record.lastWorkTick = tick;

            bool endless = EndlessWorkGiverUtility.IsEndless(workGiver);
            if (endless)
            {
                record.endlessJobCount++;
            }
            else
            {
                record.jobCount++;
            }

            int hour = tick / WorkMonitorSettings.TicksPerHour;
            HourlyWorkBucket pawnHourBucket = GetPawnWorkGiverHistory(pawn.thingIDNumber, workGiver.defName).GetOrCreateBucket(hour);
            if (endless)
            {
                pawnHourBucket.endlessJobCount++;
                if (!pawnHourBucket.pawnEndlessJobCount.ContainsKey(pawn.thingIDNumber))
                {
                    pawnHourBucket.pawnEndlessJobCount[pawn.thingIDNumber] = 0;
                }

                pawnHourBucket.pawnEndlessJobCount[pawn.thingIDNumber]++;
            }
            else
            {
                pawnHourBucket.jobCount++;
                if (!pawnHourBucket.pawnJobCount.ContainsKey(pawn.thingIDNumber))
                {
                    pawnHourBucket.pawnJobCount[pawn.thingIDNumber] = 0;
                }

                pawnHourBucket.pawnJobCount[pawn.thingIDNumber]++;
            }

            foreach (string groupKey in WorkGroupKeyResolver.ResolveGroupKeysForWorkGiver(workGiver))
            {
                HourlyWorkBucket groupBucket = GetGroupHistory(groupKey).GetOrCreateBucket(hour);
                if (endless)
                {
                    groupBucket.endlessJobCount++;
                }
                else
                {
                    groupBucket.jobCount++;
                }
            }

            if (job?.bill != null)
            {
                activeJobs[pawn.thingIDNumber] = new ActiveWorkJob
                {
                    workGiverDefName = workGiver.defName,
                    startTick = tick,
                    trackingMode = WorkLeftTrackingMode.BillIncremental,
                    lastBillWorkLeft = -1f,
                    accumulatedWorkUnits = 0f,
                    tracksWorkLeft = false,
                    startWorkLeft = 0f
                };
            }
            else
            {
                bool tracksWorkLeft = WorkLeftResolver.TryGetWorkLeft(job, pawn, out float startWorkLeft);
                activeJobs[pawn.thingIDNumber] = new ActiveWorkJob
                {
                    workGiverDefName = workGiver.defName,
                    startTick = tick,
                    trackingMode = WorkLeftTrackingMode.Snapshot,
                    tracksWorkLeft = tracksWorkLeft,
                    startWorkLeft = startWorkLeft
                };
            }
        }

        public void RecordJobEnd(Pawn pawn, WorkGiverDef workGiver, Job endingJob, int tick)
        {
            if (pawn == null)
            {
                return;
            }

            SampleBillWorkLeft(pawn, tick);

            if (workGiver == null && activeJobs.TryGetValue(pawn.thingIDNumber, out ActiveWorkJob active))
            {
                workGiver = DefDatabase<WorkGiverDef>.GetNamedSilentFail(active.workGiverDefName);
            }

            FinalizeActiveJob(pawn, tick, workGiver, endingJob);
        }

        public void SampleBillWorkLeft(Pawn pawn, int tick)
        {
            if (pawn == null || !activeJobs.TryGetValue(pawn.thingIDNumber, out ActiveWorkJob active))
            {
                return;
            }

            if (active.trackingMode != WorkLeftTrackingMode.BillIncremental)
            {
                return;
            }

            if (!WorkLeftResolver.TryGetBillDriverWorkLeft(pawn, out float currentWorkLeft))
            {
                return;
            }

            WorkGiverDef workGiver = DefDatabase<WorkGiverDef>.GetNamedSilentFail(active.workGiverDefName);
            if (workGiver == null)
            {
                return;
            }

            if (active.lastBillWorkLeft < 0f)
            {
                active.lastBillWorkLeft = currentWorkLeft;
                return;
            }

            if (currentWorkLeft < active.lastBillWorkLeft)
            {
                float delta = active.lastBillWorkLeft - currentWorkLeft;
                active.lastBillWorkLeft = currentWorkLeft;
                active.accumulatedWorkUnits += delta;
                CreditWorkUnits(pawn, workGiver, tick, delta);
            }
            else if (currentWorkLeft > active.lastBillWorkLeft)
            {
                active.lastBillWorkLeft = currentWorkLeft;
            }
        }

        public void SampleJobTick(Pawn pawn, int tick)
        {
            if (pawn == null || !pawn.IsColonist || !activeJobs.ContainsKey(pawn.thingIDNumber))
            {
                return;
            }

            if (IsTravelTick(pawn))
            {
                activeJobs[pawn.thingIDNumber].travelTicks++;
            }
            else
            {
                activeJobs[pawn.thingIDNumber].workTicks++;
            }
        }

        public int SumPawnWorkGiverTravelTicks(int pawnId, string workGiverDefName, int minHourIndex)
        {
            if (!pawnWorkGiverHistory.TryGetValue(pawnId, out Dictionary<string, WorkHistoryTierBuffer> perWg)
                || !perWg.TryGetValue(workGiverDefName, out WorkHistoryTierBuffer buffer))
            {
                return 0;
            }

            return buffer.SumPawnTravelTicks(pawnId, minHourIndex);
        }

        public int SumPawnWorkGiverWorkTicks(int pawnId, string workGiverDefName, int minHourIndex)
        {
            if (!pawnWorkGiverHistory.TryGetValue(pawnId, out Dictionary<string, WorkHistoryTierBuffer> perWg)
                || !perWg.TryGetValue(workGiverDefName, out WorkHistoryTierBuffer buffer))
            {
                return 0;
            }

            return buffer.SumPawnWorkTicks(pawnId, minHourIndex);
        }

        public WorkActivityRecord GetRecord(int pawnId, string workGiverDefName)
        {
            if (pawnRecords.TryGetValue(pawnId, out Dictionary<string, WorkActivityRecord> perWg)
                && perWg.TryGetValue(workGiverDefName, out WorkActivityRecord record))
            {
                return record;
            }

            return null;
        }

        public int SumPawnWorkGiverEndlessJobs(int pawnId, string workGiverDefName, int minHourIndex)
        {
            if (!pawnWorkGiverHistory.TryGetValue(pawnId, out Dictionary<string, WorkHistoryTierBuffer> perWg)
                || !perWg.TryGetValue(workGiverDefName, out WorkHistoryTierBuffer buffer))
            {
                return 0;
            }

            return buffer.SumEndlessJobCount(minHourIndex);
        }

        public int SumPawnWorkGiverJobs(int pawnId, string workGiverDefName, int minHourIndex)
        {
            if (!pawnWorkGiverHistory.TryGetValue(pawnId, out Dictionary<string, WorkHistoryTierBuffer> perWg)
                || !perWg.TryGetValue(workGiverDefName, out WorkHistoryTierBuffer buffer))
            {
                return 0;
            }

            return buffer.SumJobCount(minHourIndex);
        }

        public int SumPawnWorkGiverTicks(int pawnId, string workGiverDefName, int minHourIndex)
        {
            if (!pawnWorkGiverHistory.TryGetValue(pawnId, out Dictionary<string, WorkHistoryTierBuffer> perWg)
                || !perWg.TryGetValue(workGiverDefName, out WorkHistoryTierBuffer buffer))
            {
                return 0;
            }

            return buffer.SumTicksSpent(minHourIndex);
        }

        public float SumPawnWorkGiverWorkUnits(int pawnId, string workGiverDefName, int minHourIndex)
        {
            if (!pawnWorkGiverHistory.TryGetValue(pawnId, out Dictionary<string, WorkHistoryTierBuffer> perWg)
                || !perWg.TryGetValue(workGiverDefName, out WorkHistoryTierBuffer buffer))
            {
                return 0f;
            }

            return buffer.SumWorkUnits(minHourIndex);
        }

        public WorkHistoryTierBuffer GetGroupHistory(string groupKey)
        {
            if (!groupHistory.TryGetValue(groupKey, out WorkHistoryTierBuffer buffer))
            {
                buffer = new WorkHistoryTierBuffer();
                buffer.Configure(GetRetentionHours());
                groupHistory[groupKey] = buffer;
            }

            return buffer;
        }

        public bool TryGetPawnWorkGiverHistory(int pawnId, string workGiverDefName, out WorkHistoryTierBuffer buffer)
        {
            buffer = null;
            if (!pawnWorkGiverHistory.TryGetValue(pawnId, out Dictionary<string, WorkHistoryTierBuffer> perWg)
                || !perWg.TryGetValue(workGiverDefName, out buffer))
            {
                return false;
            }

            return true;
        }

        public void PruneStaleData()
        {
            int retentionHours = WorkMonitorMod.Settings?.ResolveRetentionHours() ?? 24;
            int minHour = WorkMonitorUtility.CurrentHourIndex() - retentionHours;
            Vector2 longitude = WorkMonitorUtility.MapLongitude();
            long absTick = Find.TickManager.TicksAbs;

            foreach (WorkHistoryTierBuffer buffer in groupHistory.Values)
            {
                buffer.RollupIfBoundaryCrossed(absTick, longitude);
                buffer.Configure(retentionHours);
                buffer.PruneBefore(minHour);
            }

            HashSet<int> activeColonistIds = new HashSet<int>();
            foreach (Pawn pawn in WorkMonitorUtility.MonitorColonists())
            {
                activeColonistIds.Add(pawn.thingIDNumber);
            }

            List<int> stalePawnIds = new List<int>();
            foreach (KeyValuePair<int, Dictionary<string, WorkHistoryTierBuffer>> pawnEntry in pawnWorkGiverHistory)
            {
                foreach (WorkHistoryTierBuffer buffer in pawnEntry.Value.Values)
                {
                    buffer.RollupIfBoundaryCrossed(absTick, longitude);
                    buffer.Configure(retentionHours);
                    buffer.PruneBefore(minHour);
                }

                if (!activeColonistIds.Contains(pawnEntry.Key))
                {
                    stalePawnIds.Add(pawnEntry.Key);
                }
            }

            foreach (int pawnId in stalePawnIds)
            {
                pawnWorkGiverHistory.Remove(pawnId);
                pawnRecords.Remove(pawnId);
            }
        }

        private int GetRetentionHours()
        {
            return WorkMonitorMod.Settings?.ResolveRetentionHours() ?? 24;
        }

        private List<PawnWorkGiverRecords> savedPawnRecords = new List<PawnWorkGiverRecords>();
        private List<PawnWorkGiverHistory> savedPawnHistory = new List<PawnWorkGiverHistory>();
        private List<GroupHistoryEntry> savedGroupHistory = new List<GroupHistoryEntry>();

        public override void ExposeData()
        {
            base.ExposeData();

            if (Scribe.mode == LoadSaveMode.Saving)
            {
                SyncToSaveLists();
            }

            Scribe_Collections.Look(ref savedPawnRecords, "savedPawnRecords", LookMode.Deep);
            Scribe_Collections.Look(ref savedPawnHistory, "savedPawnHistory", LookMode.Deep);
            Scribe_Collections.Look(ref savedGroupHistory, "savedGroupHistory", LookMode.Deep);

            savedPawnRecords ??= new List<PawnWorkGiverRecords>();
            savedPawnHistory ??= new List<PawnWorkGiverHistory>();
            savedGroupHistory ??= new List<GroupHistoryEntry>();

            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                SyncFromSaveLists();
            }

            activeJobs = new Dictionary<int, ActiveWorkJob>();
        }

        private void SyncToSaveLists()
        {
            savedPawnRecords = new List<PawnWorkGiverRecords>();
            foreach (KeyValuePair<int, Dictionary<string, WorkActivityRecord>> pawnEntry in pawnRecords)
            {
                savedPawnRecords.Add(new PawnWorkGiverRecords
                {
                    pawnId = pawnEntry.Key,
                    records = pawnEntry.Value
                });
            }

            savedPawnHistory = new List<PawnWorkGiverHistory>();
            foreach (KeyValuePair<int, Dictionary<string, WorkHistoryTierBuffer>> pawnEntry in pawnWorkGiverHistory)
            {
                savedPawnHistory.Add(new PawnWorkGiverHistory
                {
                    pawnId = pawnEntry.Key,
                    history = pawnEntry.Value
                });
            }

            savedGroupHistory = new List<GroupHistoryEntry>();
            foreach (KeyValuePair<string, WorkHistoryTierBuffer> groupEntry in groupHistory)
            {
                savedGroupHistory.Add(new GroupHistoryEntry
                {
                    groupKey = groupEntry.Key,
                    buffer = groupEntry.Value
                });
            }
        }

        private void SyncFromSaveLists()
        {
            pawnRecords = new Dictionary<int, Dictionary<string, WorkActivityRecord>>();
            foreach (PawnWorkGiverRecords entry in savedPawnRecords)
            {
                pawnRecords[entry.pawnId] = entry.records ?? new Dictionary<string, WorkActivityRecord>();
            }

            pawnWorkGiverHistory = new Dictionary<int, Dictionary<string, WorkHistoryTierBuffer>>();
            foreach (PawnWorkGiverHistory entry in savedPawnHistory)
            {
                pawnWorkGiverHistory[entry.pawnId] = entry.history ?? new Dictionary<string, WorkHistoryTierBuffer>();
            }

            groupHistory = new Dictionary<string, WorkHistoryTierBuffer>();
            foreach (GroupHistoryEntry entry in savedGroupHistory)
            {
                if (!entry.groupKey.NullOrEmpty())
                {
                    groupHistory[entry.groupKey] = entry.buffer ?? new WorkHistoryTierBuffer();
                }
            }
        }

        private void FinalizeActiveJob(Pawn pawn, int tick, WorkGiverDef explicitWorkGiver, Job endingJob)
        {
            if (!activeJobs.TryGetValue(pawn.thingIDNumber, out ActiveWorkJob active))
            {
                return;
            }

            SampleBillWorkLeft(pawn, tick);

            WorkGiverDef workGiver = explicitWorkGiver ?? DefDatabase<WorkGiverDef>.GetNamedSilentFail(active.workGiverDefName);
            if (workGiver == null)
            {
                activeJobs.Remove(pawn.thingIDNumber);
                return;
            }

            int elapsed = Mathf.Max(0, tick - active.startTick);
            int travelTicks = active.travelTicks;
            int workTicks = active.workTicks;
            if (travelTicks + workTicks <= 0)
            {
                workTicks = elapsed;
            }
            else if (travelTicks + workTicks != elapsed)
            {
                int diff = elapsed - (travelTicks + workTicks);
                if (diff > 0)
                {
                    workTicks += diff;
                }
            }

            WorkActivityRecord record = GetOrCreateRecord(pawn.thingIDNumber, workGiver.defName);
            record.ticksSpent += elapsed;
            record.travelTicksSpent += travelTicks;
            record.workTicksSpent += workTicks;

            int hour = tick / WorkMonitorSettings.TicksPerHour;
            HourlyWorkBucket pawnBucket = GetPawnWorkGiverHistory(pawn.thingIDNumber, workGiver.defName).GetOrCreateBucket(hour);
            if (travelTicks > 0)
            {
                pawnBucket.AddTravelTicks(pawn.thingIDNumber, travelTicks);
            }

            if (workTicks > 0)
            {
                pawnBucket.AddWorkTicks(pawn.thingIDNumber, workTicks);
            }

            float workDelta = 0f;
            bool estimated = false;
            if (active.trackingMode == WorkLeftTrackingMode.Snapshot
                && active.tracksWorkLeft
                && WorkLeftResolver.TryGetWorkLeft(endingJob, pawn, out float endWorkLeft))
            {
                workDelta = Mathf.Max(0f, active.startWorkLeft - endWorkLeft);
            }

            if (workDelta <= 0f
                && workTicks > 0
                && active.trackingMode != WorkLeftTrackingMode.BillIncremental
                && active.accumulatedWorkUnits <= 0f
                && WorkUnitEstimator.TryEstimateWorkUnits(pawn, workGiver, endingJob, workTicks, out float estimatedUnits))
            {
                workDelta = estimatedUnits;
                estimated = true;
            }

            if (workDelta > 0f)
            {
                if (estimated)
                {
                    CreditEstimatedWorkUnits(pawn, workGiver, tick, workDelta);
                }
                else
                {
                    record.workUnitsSpent += workDelta;
                    pawnBucket.AddWorkUnits(pawn.thingIDNumber, workDelta);
                    CreditGroupWorkUnits(pawn, workGiver, tick, workDelta, estimated: false);
                }
            }

            foreach (string groupKey in WorkGroupKeyResolver.ResolveGroupKeysForWorkGiver(workGiver))
            {
                HourlyWorkBucket groupBucket = GetGroupHistory(groupKey).GetOrCreateBucket(hour);
                if (travelTicks > 0)
                {
                    groupBucket.AddTravelTicks(pawn.thingIDNumber, travelTicks);
                }

                if (workTicks > 0)
                {
                    groupBucket.AddWorkTicks(pawn.thingIDNumber, workTicks);
                }
            }

            activeJobs.Remove(pawn.thingIDNumber);
        }

        private void CreditWorkUnits(Pawn pawn, WorkGiverDef workGiver, int tick, float units)
        {
            if (units <= 0f)
            {
                return;
            }

            WorkActivityRecord record = GetOrCreateRecord(pawn.thingIDNumber, workGiver.defName);
            record.workUnitsSpent += units;

            int hour = tick / WorkMonitorSettings.TicksPerHour;
            HourlyWorkBucket pawnBucket = GetPawnWorkGiverHistory(pawn.thingIDNumber, workGiver.defName).GetOrCreateBucket(hour);
            pawnBucket.AddWorkUnits(pawn.thingIDNumber, units);
            CreditGroupWorkUnits(pawn, workGiver, tick, units, estimated: false);
        }

        private void CreditEstimatedWorkUnits(Pawn pawn, WorkGiverDef workGiver, int tick, float units)
        {
            if (units <= 0f)
            {
                return;
            }

            WorkActivityRecord record = GetOrCreateRecord(pawn.thingIDNumber, workGiver.defName);
            record.estimatedWorkUnitsSpent += units;

            int hour = tick / WorkMonitorSettings.TicksPerHour;
            HourlyWorkBucket pawnBucket = GetPawnWorkGiverHistory(pawn.thingIDNumber, workGiver.defName).GetOrCreateBucket(hour);
            pawnBucket.AddEstimatedWorkUnits(pawn.thingIDNumber, units);
            CreditGroupWorkUnits(pawn, workGiver, tick, units, estimated: true);
        }

        private void CreditGroupWorkUnits(Pawn pawn, WorkGiverDef workGiver, int tick, float units, bool estimated)
        {
            int hour = tick / WorkMonitorSettings.TicksPerHour;
            foreach (string groupKey in WorkGroupKeyResolver.ResolveGroupKeysForWorkGiver(workGiver))
            {
                HourlyWorkBucket groupBucket = GetGroupHistory(groupKey).GetOrCreateBucket(hour);
                if (estimated)
                {
                    groupBucket.AddEstimatedWorkUnits(pawn.thingIDNumber, units);
                }
                else
                {
                    groupBucket.AddWorkUnits(pawn.thingIDNumber, units);
                }
            }
        }

        private WorkHistoryTierBuffer GetPawnWorkGiverHistory(int pawnId, string workGiverDefName)
        {
            if (!pawnWorkGiverHistory.TryGetValue(pawnId, out Dictionary<string, WorkHistoryTierBuffer> perWg))
            {
                perWg = new Dictionary<string, WorkHistoryTierBuffer>();
                pawnWorkGiverHistory[pawnId] = perWg;
            }

            if (!perWg.TryGetValue(workGiverDefName, out WorkHistoryTierBuffer buffer))
            {
                buffer = new WorkHistoryTierBuffer();
                buffer.Configure(GetRetentionHours());
                perWg[workGiverDefName] = buffer;
            }

            return buffer;
        }

        private WorkActivityRecord GetOrCreateRecord(int pawnId, string workGiverDefName)
        {
            if (!pawnRecords.TryGetValue(pawnId, out Dictionary<string, WorkActivityRecord> perWg))
            {
                perWg = new Dictionary<string, WorkActivityRecord>();
                pawnRecords[pawnId] = perWg;
            }

            if (!perWg.TryGetValue(workGiverDefName, out WorkActivityRecord record))
            {
                record = new WorkActivityRecord();
                perWg[workGiverDefName] = record;
            }

            return record;
        }

        private static bool IsTravelTick(Pawn pawn)
        {
            return pawn?.pather?.MovingNow == true;
        }
    }
}
