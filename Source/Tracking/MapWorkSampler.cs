using System.Collections.Generic;
using RimWorld;
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
            latestSnapshot = BuildSnapshot(map, hour, Find.TickManager.TicksGame);
            historyBuffer.Add(latestSnapshot);
            while (historyBuffer.Count > 48)
            {
                historyBuffer.RemoveAt(0);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref latestSnapshot, "latestSnapshot");
            Scribe_Collections.Look(ref historyBuffer, "historyBuffer", LookMode.Deep);
            Scribe_Values.Look(ref lastSampledHour, "lastSampledHour", -1);
            historyBuffer ??= new List<MapWorkSnapshot>();
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

        private static MapWorkSnapshot BuildSnapshot(Map map, int hourIndex, int sampleTick)
        {
            MapWorkSnapshot snapshot = new MapWorkSnapshot
            {
                hourIndex = hourIndex,
                sampleTick = sampleTick
            };

            List<ScannedMapTarget> targets = new List<ScannedMapTarget>();
            HashSet<string> seenKeys = new HashSet<string>();

            foreach (IMapWorkTargetProvider provider in MapWorkProviderRegistry.All)
            {
                provider.Collect(map, targets);
            }

            foreach (ScannedMapTarget target in targets)
            {
                if (!seenKeys.Add(target.DedupeKey))
                {
                    continue;
                }

                foreach (string workGiverDefName in target.WorkGiverDefNames)
                {
                    MapWorkGiverSnapshot wgSnap = snapshot.GetOrCreateWorkGiver(workGiverDefName);
                    wgSnap.openTaskCount++;
                    wgSnap.workLeftTotal += target.WorkLeft;
                }

                foreach (string groupKey in target.GroupKeys)
                {
                    MapWorkGroupSnapshot groupSnap = snapshot.GetOrCreateGroup(groupKey);
                    groupSnap.openTaskCount++;
                    groupSnap.workLeftTotal += target.WorkLeft;
                }
            }

            return snapshot;
        }
    }
}
