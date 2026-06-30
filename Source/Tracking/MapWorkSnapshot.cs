using System.Collections.Generic;
using Verse;

namespace WorkMonitor.Tracking
{
    public class MapWorkGiverSnapshot : IExposable
    {
        public string workGiverDefName;
        public int openTaskCount;
        public int newTodayOpenTaskCount;
        public float workLeftTotal;
        public float newTodayWorkLeftTotal;

        public void ExposeData()
        {
            Scribe_Values.Look(ref workGiverDefName, "workGiverDefName");
            Scribe_Values.Look(ref openTaskCount, "openTaskCount");
            Scribe_Values.Look(ref newTodayOpenTaskCount, "newTodayOpenTaskCount", 0);
            Scribe_Values.Look(ref workLeftTotal, "workLeftTotal");
            Scribe_Values.Look(ref newTodayWorkLeftTotal, "newTodayWorkLeftTotal", 0f);
        }
    }

    public class MapWorkGroupSnapshot : IExposable
    {
        public string groupStorageKey;
        public int openTaskCount;
        public int newTodayOpenTaskCount;
        public float workLeftTotal;
        public float newTodayWorkLeftTotal;

        public void ExposeData()
        {
            Scribe_Values.Look(ref groupStorageKey, "groupStorageKey");
            Scribe_Values.Look(ref openTaskCount, "openTaskCount");
            Scribe_Values.Look(ref newTodayOpenTaskCount, "newTodayOpenTaskCount", 0);
            Scribe_Values.Look(ref workLeftTotal, "workLeftTotal");
            Scribe_Values.Look(ref newTodayWorkLeftTotal, "newTodayWorkLeftTotal", 0f);
        }
    }

    public class MapWorkSnapshot : IExposable
    {
        public int hourIndex;
        public int sampleTick;
        public Dictionary<string, MapWorkGiverSnapshot> perWorkGiver = new Dictionary<string, MapWorkGiverSnapshot>();
        public Dictionary<string, MapWorkGroupSnapshot> perGroupKey = new Dictionary<string, MapWorkGroupSnapshot>();

        public MapWorkGiverSnapshot GetOrCreateWorkGiver(string workGiverDefName)
        {
            if (!perWorkGiver.TryGetValue(workGiverDefName, out MapWorkGiverSnapshot snap))
            {
                snap = new MapWorkGiverSnapshot { workGiverDefName = workGiverDefName };
                perWorkGiver[workGiverDefName] = snap;
            }

            return snap;
        }

        public MapWorkGroupSnapshot GetOrCreateGroup(string groupKey)
        {
            if (!perGroupKey.TryGetValue(groupKey, out MapWorkGroupSnapshot snap))
            {
                snap = new MapWorkGroupSnapshot { groupStorageKey = groupKey };
                perGroupKey[groupKey] = snap;
            }

            return snap;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref hourIndex, "hourIndex");
            Scribe_Values.Look(ref sampleTick, "sampleTick");
            Scribe_Collections.Look(ref perWorkGiver, "perWorkGiver", LookMode.Value, LookMode.Deep);
            Scribe_Collections.Look(ref perGroupKey, "perGroupKey", LookMode.Value, LookMode.Deep);
            perWorkGiver ??= new Dictionary<string, MapWorkGiverSnapshot>();
            perGroupKey ??= new Dictionary<string, MapWorkGroupSnapshot>();
        }
    }
}
