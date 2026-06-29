using System.Collections.Generic;
using Verse;

namespace WorkMonitor.Tracking
{
    public class HourlyWorkBucket : IExposable
    {
        public int hourIndex;
        public int jobCount;
        public int ticksSpent;
        public Dictionary<int, int> pawnTicksSpent = new Dictionary<int, int>();

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

        public void ExposeData()
        {
            Scribe_Values.Look(ref hourIndex, "hourIndex");
            Scribe_Values.Look(ref jobCount, "jobCount");
            Scribe_Values.Look(ref ticksSpent, "ticksSpent");
            Scribe_Collections.Look(ref pawnTicksSpent, "pawnTicksSpent", LookMode.Value, LookMode.Value);
            pawnTicksSpent ??= new Dictionary<int, int>();
        }
    }
}
