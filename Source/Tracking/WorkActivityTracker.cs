using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using WorkMonitor.Groups;

namespace WorkMonitor.Tracking
{
    public class ActiveWorkJob
    {
        public string workGiverDefName;
        public int startTick;
    }

    public class WorkActivityTracker : GameComponent
    {
        private static WorkActivityTracker instance;

        private Dictionary<int, Dictionary<string, WorkActivityRecord>> pawnRecords =
            new Dictionary<int, Dictionary<string, WorkActivityRecord>>();

        private Dictionary<int, Dictionary<string, WorkHistoryRingBuffer>> pawnWorkGiverHistory =
            new Dictionary<int, Dictionary<string, WorkHistoryRingBuffer>>();

        private Dictionary<int, ActiveWorkJob> activeJobs = new Dictionary<int, ActiveWorkJob>();

        private Dictionary<string, WorkHistoryRingBuffer> groupHistory =
            new Dictionary<string, WorkHistoryRingBuffer>();

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

        public void RecordJobStart(Pawn pawn, WorkGiverDef workGiver, int tick)
        {
            if (pawn == null || workGiver == null || !pawn.IsColonist)
            {
                return;
            }

            FinalizeActiveJob(pawn, tick);

            WorkActivityRecord record = GetOrCreateRecord(pawn.thingIDNumber, workGiver.defName);
            record.lastWorkTick = tick;
            record.jobCount++;

            int hour = tick / WorkMonitorSettings.TicksPerHour;
            GetPawnWorkGiverHistory(pawn.thingIDNumber, workGiver.defName).GetOrCreateBucket(hour).jobCount++;

            foreach (string groupKey in WorkGroupKeyResolver.ResolveGroupKeysForWorkGiver(workGiver))
            {
                GetGroupHistory(groupKey).GetOrCreateBucket(hour).jobCount++;
            }

            activeJobs[pawn.thingIDNumber] = new ActiveWorkJob
            {
                workGiverDefName = workGiver.defName,
                startTick = tick
            };
        }

        public void RecordJobEnd(Pawn pawn, WorkGiverDef workGiver, int tick)
        {
            if (pawn == null)
            {
                return;
            }

            if (workGiver == null && activeJobs.TryGetValue(pawn.thingIDNumber, out ActiveWorkJob active))
            {
                workGiver = DefDatabase<WorkGiverDef>.GetNamedSilentFail(active.workGiverDefName);
            }

            FinalizeActiveJob(pawn, tick, workGiver);
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

        public int SumPawnWorkGiverJobs(int pawnId, string workGiverDefName, int minHourIndex)
        {
            if (!pawnWorkGiverHistory.TryGetValue(pawnId, out Dictionary<string, WorkHistoryRingBuffer> perWg)
                || !perWg.TryGetValue(workGiverDefName, out WorkHistoryRingBuffer buffer))
            {
                return 0;
            }

            return buffer.SumJobCount(minHourIndex);
        }

        public int SumPawnWorkGiverTicks(int pawnId, string workGiverDefName, int minHourIndex)
        {
            if (!pawnWorkGiverHistory.TryGetValue(pawnId, out Dictionary<string, WorkHistoryRingBuffer> perWg)
                || !perWg.TryGetValue(workGiverDefName, out WorkHistoryRingBuffer buffer))
            {
                return 0;
            }

            return buffer.SumTicksSpent(minHourIndex);
        }

        public WorkHistoryRingBuffer GetGroupHistory(string groupKey)
        {
            if (!groupHistory.TryGetValue(groupKey, out WorkHistoryRingBuffer buffer))
            {
                buffer = new WorkHistoryRingBuffer();
                buffer.Configure(WorkMonitorMod.Settings?.chartHistoryHours ?? 24);
                groupHistory[groupKey] = buffer;
            }

            return buffer;
        }

        public void PruneStaleData()
        {
            int chartHours = WorkMonitorMod.Settings?.chartHistoryHours ?? 24;
            int minHour = WorkMonitorUtility.CurrentHourIndex() - chartHours;

            foreach (WorkHistoryRingBuffer buffer in groupHistory.Values)
            {
                buffer.Configure(chartHours);
                buffer.PruneBefore(minHour);
            }

            foreach (KeyValuePair<int, Dictionary<string, WorkHistoryRingBuffer>> pawnEntry in pawnWorkGiverHistory)
            {
                foreach (WorkHistoryRingBuffer buffer in pawnEntry.Value.Values)
                {
                    buffer.Configure(chartHours);
                    buffer.PruneBefore(minHour);
                }
            }
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
            foreach (KeyValuePair<int, Dictionary<string, WorkHistoryRingBuffer>> pawnEntry in pawnWorkGiverHistory)
            {
                savedPawnHistory.Add(new PawnWorkGiverHistory
                {
                    pawnId = pawnEntry.Key,
                    history = pawnEntry.Value
                });
            }

            savedGroupHistory = new List<GroupHistoryEntry>();
            foreach (KeyValuePair<string, WorkHistoryRingBuffer> groupEntry in groupHistory)
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

            pawnWorkGiverHistory = new Dictionary<int, Dictionary<string, WorkHistoryRingBuffer>>();
            foreach (PawnWorkGiverHistory entry in savedPawnHistory)
            {
                pawnWorkGiverHistory[entry.pawnId] = entry.history ?? new Dictionary<string, WorkHistoryRingBuffer>();
            }

            groupHistory = new Dictionary<string, WorkHistoryRingBuffer>();
            foreach (GroupHistoryEntry entry in savedGroupHistory)
            {
                if (!entry.groupKey.NullOrEmpty())
                {
                    groupHistory[entry.groupKey] = entry.buffer ?? new WorkHistoryRingBuffer();
                }
            }
        }

        private void FinalizeActiveJob(Pawn pawn, int tick, WorkGiverDef explicitWorkGiver = null)
        {
            if (!activeJobs.TryGetValue(pawn.thingIDNumber, out ActiveWorkJob active))
            {
                return;
            }

            WorkGiverDef workGiver = explicitWorkGiver ?? DefDatabase<WorkGiverDef>.GetNamedSilentFail(active.workGiverDefName);
            if (workGiver == null)
            {
                activeJobs.Remove(pawn.thingIDNumber);
                return;
            }

            int elapsed = Mathf.Max(0, tick - active.startTick);
            WorkActivityRecord record = GetOrCreateRecord(pawn.thingIDNumber, workGiver.defName);
            record.ticksSpent += elapsed;

            int hour = tick / WorkMonitorSettings.TicksPerHour;
            HourlyWorkBucket pawnBucket = GetPawnWorkGiverHistory(pawn.thingIDNumber, workGiver.defName).GetOrCreateBucket(hour);
            pawnBucket.ticksSpent += elapsed;
            if (!pawnBucket.pawnTicksSpent.ContainsKey(pawn.thingIDNumber))
            {
                pawnBucket.pawnTicksSpent[pawn.thingIDNumber] = 0;
            }

            pawnBucket.pawnTicksSpent[pawn.thingIDNumber] += elapsed;

            foreach (string groupKey in WorkGroupKeyResolver.ResolveGroupKeysForWorkGiver(workGiver))
            {
                HourlyWorkBucket groupBucket = GetGroupHistory(groupKey).GetOrCreateBucket(hour);
                groupBucket.ticksSpent += elapsed;
                if (!groupBucket.pawnTicksSpent.ContainsKey(pawn.thingIDNumber))
                {
                    groupBucket.pawnTicksSpent[pawn.thingIDNumber] = 0;
                }

                groupBucket.pawnTicksSpent[pawn.thingIDNumber] += elapsed;
            }

            activeJobs.Remove(pawn.thingIDNumber);
        }

        private WorkHistoryRingBuffer GetPawnWorkGiverHistory(int pawnId, string workGiverDefName)
        {
            if (!pawnWorkGiverHistory.TryGetValue(pawnId, out Dictionary<string, WorkHistoryRingBuffer> perWg))
            {
                perWg = new Dictionary<string, WorkHistoryRingBuffer>();
                pawnWorkGiverHistory[pawnId] = perWg;
            }

            if (!perWg.TryGetValue(workGiverDefName, out WorkHistoryRingBuffer buffer))
            {
                buffer = new WorkHistoryRingBuffer();
                buffer.Configure(WorkMonitorMod.Settings?.chartHistoryHours ?? 24);
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
    }
}
