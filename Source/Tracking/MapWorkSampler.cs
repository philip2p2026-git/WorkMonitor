using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using WorkMonitor.Tracking.MapWork;

namespace WorkMonitor.Tracking
{
    public class MapWorkSampler : GameComponent
    {
        private static MapWorkSampler instance;

        private MapWorkSnapshot latestSnapshot;
        private int lastSampledHour = -1;
        private List<MapWorkSnapshot> historyBuffer = new List<MapWorkSnapshot>();
        private Dictionary<string, int> taskFirstSeenDayId = new Dictionary<string, int>();

        public static MapWorkSampler Instance
        {
            get
            {
                if (instance == null && Current.Game != null)
                {
                    instance = Current.Game.GetComponent<MapWorkSampler>();
                }

                return instance;
            }
        }

        public static MapWorkSampler EnsureRegistered()
        {
            if (Current.Game == null)
            {
                return null;
            }

            MapWorkSampler sampler = Current.Game.GetComponent<MapWorkSampler>();
            if (sampler == null)
            {
                sampler = new MapWorkSampler(Current.Game);
                Current.Game.components.Add(sampler);
            }

            instance = sampler;
            return sampler;
        }

        public static void ClearInstance()
        {
            instance = null;
        }

        public MapWorkSampler(Game game)
        {
            instance = this;
        }

        public MapWorkSnapshot GetLatestSnapshot()
        {
            return latestSnapshot;
        }

        public IReadOnlyList<MapWorkSnapshot> GetHistory()
        {
            return historyBuffer;
        }

        public override void GameComponentTick()
        {
            if (Find.TickManager.TicksGame % 250 != 0)
            {
                return;
            }

            TrySampleIfDue();
        }

        public void TrySampleIfDue(bool force = false)
        {
            Map map = Find.CurrentMap;
            if (map == null)
            {
                return;
            }

            int hour = WorkMonitorUtility.CurrentHourIndex();
            int interval = NormalizeInterval(WorkMonitorMod.Settings?.mapSampleIntervalHours ?? 6);
            if (!force && hour % interval != 0)
            {
                return;
            }

            if (!force && lastSampledHour == hour)
            {
                return;
            }

            lastSampledHour = hour;
            latestSnapshot = BuildSnapshot(map, hour, Find.TickManager.TicksGame, Find.TickManager.TicksAbs);
            historyBuffer.Add(latestSnapshot);
            int minHour = WorkMonitorUtility.CurrentHourIndex() - WorkMonitorSettings.MaxRetentionHours;
            while (historyBuffer.Count > 0 && historyBuffer[0].hourIndex < minHour)
            {
                historyBuffer.RemoveAt(0);
            }

            int maxHistory = WorkMonitorSettings.MaxRetentionHours + 8;
            while (historyBuffer.Count > maxHistory)
            {
                historyBuffer.RemoveAt(0);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref latestSnapshot, "latestSnapshot");
            Scribe_Collections.Look(ref historyBuffer, "historyBuffer", LookMode.Deep);
            Scribe_Collections.Look(ref taskFirstSeenDayId, "taskFirstSeenDayId", LookMode.Value, LookMode.Value);
            Scribe_Values.Look(ref lastSampledHour, "lastSampledHour", -1);
            historyBuffer ??= new List<MapWorkSnapshot>();
            taskFirstSeenDayId ??= new Dictionary<string, int>();
        }

        public static int NormalizeInterval(int hours)
        {
            return hours switch
            {
                1 => 1,
                2 => 2,
                3 => 3,
                12 => 12,
                _ => 6
            };
        }

        private MapWorkSnapshot BuildSnapshot(Map map, int hourIndex, int sampleTick, long absTick)
        {
            MapWorkSnapshot snapshot = new MapWorkSnapshot
            {
                hourIndex = hourIndex,
                sampleTick = sampleTick
            };

            List<ScannedMapTarget> targets = new List<ScannedMapTarget>();
            HashSet<string> currentKeys = new HashSet<string>();
            HashSet<string> seenKeys = new HashSet<string>();

            foreach (IMapWorkTargetProvider provider in MapWorkProviderRegistry.All)
            {
                provider.Collect(map, targets);
            }

            Vector2 longitude = Find.WorldGrid.LongLatOf(map.Tile);
            int rolloverHour = WorkMonitorUtility.DayRolloverHour();
            int currentDayId = WorkMonitorUtility.GetWorkDayId(absTick, longitude, rolloverHour);

            foreach (ScannedMapTarget target in targets)
            {
                if (!seenKeys.Add(target.DedupeKey))
                {
                    continue;
                }

                currentKeys.Add(target.DedupeKey);
                if (!taskFirstSeenDayId.ContainsKey(target.DedupeKey))
                {
                    taskFirstSeenDayId[target.DedupeKey] = currentDayId;
                }

                bool isNewToday = taskFirstSeenDayId[target.DedupeKey] == currentDayId;

                foreach (string workGiverDefName in target.WorkGiverDefNames)
                {
                    MapWorkGiverSnapshot wgSnap = snapshot.GetOrCreateWorkGiver(workGiverDefName);
                    wgSnap.openTaskCount++;
                    wgSnap.workLeftTotal += target.WorkLeft;
                    if (isNewToday)
                    {
                        wgSnap.newTodayOpenTaskCount++;
                        wgSnap.newTodayWorkLeftTotal += target.WorkLeft;
                    }
                }

                foreach (string groupKey in target.GroupKeys)
                {
                    MapWorkGroupSnapshot groupSnap = snapshot.GetOrCreateGroup(groupKey);
                    groupSnap.openTaskCount++;
                    groupSnap.workLeftTotal += target.WorkLeft;
                    if (isNewToday)
                    {
                        groupSnap.newTodayOpenTaskCount++;
                        groupSnap.newTodayWorkLeftTotal += target.WorkLeft;
                    }
                }
            }

            List<string> staleKeys = new List<string>();
            foreach (KeyValuePair<string, int> entry in taskFirstSeenDayId)
            {
                if (!currentKeys.Contains(entry.Key) && entry.Value < currentDayId - 1)
                {
                    staleKeys.Add(entry.Key);
                }
            }

            foreach (string key in staleKeys)
            {
                taskFirstSeenDayId.Remove(key);
            }

            return snapshot;
        }
    }
}
