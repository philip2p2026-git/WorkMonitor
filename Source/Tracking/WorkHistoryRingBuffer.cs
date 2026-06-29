using System.Collections.Generic;
using System.Linq;
using Verse;

namespace WorkMonitor.Tracking
{
    public class WorkHistoryRingBuffer : IExposable
    {
        private List<HourlyWorkBucket> buckets = new List<HourlyWorkBucket>();
        private int maxHours = 24;

        public IReadOnlyList<HourlyWorkBucket> Buckets => buckets;

        public void Configure(int hours)
        {
            maxHours = hours;
            Trim();
        }

        public HourlyWorkBucket GetOrCreateBucket(int hourIndex)
        {
            HourlyWorkBucket existing = buckets.FirstOrDefault(b => b.hourIndex == hourIndex);
            if (existing != null)
            {
                return existing;
            }

            var bucket = new HourlyWorkBucket { hourIndex = hourIndex };
            buckets.Add(bucket);
            buckets.Sort((a, b) => a.hourIndex.CompareTo(b.hourIndex));
            Trim();
            return bucket;
        }

        public void PruneBefore(int minHourIndex)
        {
            buckets.RemoveAll(b => b.hourIndex < minHourIndex);
        }

        public int SumJobCount(int minHourIndex)
        {
            return buckets.Where(b => b.hourIndex >= minHourIndex).Sum(b => b.jobCount);
        }

        public int SumTicksSpent(int minHourIndex)
        {
            return buckets.Where(b => b.hourIndex >= minHourIndex).Sum(b => b.ticksSpent);
        }

        public float SumWorkUnits(int minHourIndex)
        {
            return buckets.Where(b => b.hourIndex >= minHourIndex).Sum(b => b.workUnitsSpent);
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref maxHours, "maxHours", 24);
            Scribe_Collections.Look(ref buckets, "buckets", LookMode.Deep);
            buckets ??= new List<HourlyWorkBucket>();
        }

        private void Trim()
        {
            if (maxHours <= 0)
            {
                return;
            }

            while (buckets.Count > maxHours)
            {
                buckets.RemoveAt(0);
            }
        }
    }
}
