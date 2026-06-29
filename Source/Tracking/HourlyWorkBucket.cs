using System.Collections.Generic;
using Verse;

namespace WorkMonitor.Tracking
{
    public class HourlyWorkBucket : IExposable
    {
        public int hourIndex;
        public int jobCount;
        public int ticksSpent;
        public int travelTicksSpent;
        public int workTicksSpent;
        public float workUnitsSpent;
        public Dictionary<int, int> pawnTicksSpent = new Dictionary<int, int>();
        public Dictionary<int, int> pawnTravelTicksSpent = new Dictionary<int, int>();
        public Dictionary<int, int> pawnWorkTicksSpent = new Dictionary<int, int>();
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

        public void AddTravelTicks(int pawnId, int ticks)
        {
            travelTicksSpent += ticks;
            ticksSpent += ticks;
            if (pawnId >= 0)
            {
                if (!pawnTravelTicksSpent.ContainsKey(pawnId))
                {
                    pawnTravelTicksSpent[pawnId] = 0;
                }

                pawnTravelTicksSpent[pawnId] += ticks;

                if (!pawnTicksSpent.ContainsKey(pawnId))
                {
                    pawnTicksSpent[pawnId] = 0;
                }

                pawnTicksSpent[pawnId] += ticks;
            }
        }

        public void AddWorkTicks(int pawnId, int ticks)
        {
            workTicksSpent += ticks;
            ticksSpent += ticks;
            if (pawnId >= 0)
            {
                if (!pawnWorkTicksSpent.ContainsKey(pawnId))
                {
                    pawnWorkTicksSpent[pawnId] = 0;
                }

                pawnWorkTicksSpent[pawnId] += ticks;

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
            Scribe_Values.Look(ref travelTicksSpent, "travelTicksSpent", 0);
            Scribe_Values.Look(ref workTicksSpent, "workTicksSpent", 0);
            Scribe_Values.Look(ref workUnitsSpent, "workUnitsSpent", 0f);
            Scribe_Collections.Look(ref pawnTicksSpent, "pawnTicksSpent", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref pawnTravelTicksSpent, "pawnTravelTicksSpent", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref pawnWorkTicksSpent, "pawnWorkTicksSpent", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref pawnWorkUnitsSpent, "pawnWorkUnitsSpent", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref pawnJobCount, "pawnJobCount", LookMode.Value, LookMode.Value);
            pawnTicksSpent ??= new Dictionary<int, int>();
            pawnTravelTicksSpent ??= new Dictionary<int, int>();
            pawnWorkTicksSpent ??= new Dictionary<int, int>();
            pawnWorkUnitsSpent ??= new Dictionary<int, float>();
            pawnJobCount ??= new Dictionary<int, int>();
        }
    }
}
