using System.Collections.Generic;
using Verse;

namespace WorkMonitor.Tracking
{
    public class HourlyWorkBucket : IExposable
    {
        public int hourIndex;
        public int jobCount;
        public int ticksSpent;
        public float workUnitsSpent;
        public Dictionary<int, int> pawnTicksSpent = new Dictionary<int, int>();
        public Dictionary<int, float> pawnWorkUnitsSpent = new Dictionary<int, float>();
        public Dictionary<int, int> pawnJobCount = new Dictionary<int, int>();

        public void AddJob(int pawnId, int ticks)
        {
            jobCount++;
            ticksSpent += ticks;
            if (pawnId >= 0)
            {
                if (!pawnTicksSpent.ContainsKey(pawnId))
                {
                    pawnTicksSpent[pawnId] = 0;
                }

                pawnTicksSpent[pawnId] += ticks;
            }
        }

        public void AddWorkUnits(int pawnId, float units)
        {
            workUnitsSpent += units;
            if (pawnId >= 0)
            {
                if (!pawnWorkUnitsSpent.ContainsKey(pawnId))
                {
                    pawnWorkUnitsSpent[pawnId] = 0f;
                }

                pawnWorkUnitsSpent[pawnId] += units;
            }
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref hourIndex, "hourIndex");
            Scribe_Values.Look(ref jobCount, "jobCount");
            Scribe_Values.Look(ref ticksSpent, "ticksSpent");
            Scribe_Values.Look(ref workUnitsSpent, "workUnitsSpent", 0f);
            Scribe_Collections.Look(ref pawnTicksSpent, "pawnTicksSpent", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref pawnWorkUnitsSpent, "pawnWorkUnitsSpent", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref pawnJobCount, "pawnJobCount", LookMode.Value, LookMode.Value);
            pawnTicksSpent ??= new Dictionary<int, int>();
            pawnWorkUnitsSpent ??= new Dictionary<int, float>();
            pawnJobCount ??= new Dictionary<int, int>();
        }
    }
}
