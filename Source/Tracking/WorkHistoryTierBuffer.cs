using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace WorkMonitor.Tracking
{
    public class WorkHistoryTierBuffer : IExposable
    {
        private List<HourlyWorkBucket> hourly = new List<HourlyWorkBucket>();
        private List<DailyWorkBucket> daily = new List<DailyWorkBucket>();
        private List<QuadrumWorkBucket> quadrums = new List<QuadrumWorkBucket>();
        private List<YearWorkBucket> years = new List<YearWorkBucket>();
        private int maxHourlyHours = WorkMonitorSettings.MaxRetentionHours;

        public IReadOnlyList<HourlyWorkBucket> Buckets => hourly;
        public IReadOnlyList<DailyWorkBucket> DailyBuckets => daily;

        public void Configure(int hourlyRetentionHours)
        {
            maxHourlyHours = Mathf.Clamp(hourlyRetentionHours, 6, WorkMonitorSettings.MaxRetentionHours);
            TrimHourly();
        }

        public HourlyWorkBucket GetOrCreateBucket(int hourIndex)
        {
            HourlyWorkBucket existing = hourly.FirstOrDefault(b => b.hourIndex == hourIndex);
            if (existing != null)
            {
                return existing;
            }

            var bucket = new HourlyWorkBucket { hourIndex = hourIndex };
            hourly.Add(bucket);
            hourly.Sort((a, b) => a.hourIndex.CompareTo(b.hourIndex));
            TrimHourly();
            return bucket;
        }

        public HourlyWorkBucket GetBucket(int hourIndex)
        {
            return hourly.FirstOrDefault(b => b.hourIndex == hourIndex);
        }

        public void PruneBefore(int minHourIndex)
        {
            hourly.RemoveAll(b => b.hourIndex < minHourIndex);
        }

        public int SumJobCount(int minHourIndex) => (int)SumFloat(minHourIndex, b => b.jobCount, d => d.jobCount, q => q.jobCount, y => y.jobCount);
        public int SumEndlessJobCount(int minHourIndex) => (int)SumFloat(minHourIndex, b => b.endlessJobCount, d => d.endlessJobCount, q => q.endlessJobCount, y => y.endlessJobCount);
        public int SumTicksSpent(int minHourIndex) => (int)SumFloat(minHourIndex, b => b.ticksSpent, d => d.ticksSpent, q => q.ticksSpent, y => y.ticksSpent);
        public float SumWorkUnits(int minHourIndex) => SumFloat(minHourIndex, b => b.workUnitsSpent + b.estimatedWorkUnitsSpent, d => d.workUnitsSpent + d.estimatedWorkUnitsSpent, q => q.workUnitsSpent + q.estimatedWorkUnitsSpent, y => y.workUnitsSpent + y.estimatedWorkUnitsSpent);
        public int SumTravelTicksSpent(int minHourIndex) => (int)SumFloat(minHourIndex, b => b.travelTicksSpent, d => d.travelTicksSpent, q => q.travelTicksSpent, y => y.travelTicksSpent);
        public int SumWorkTicksSpent(int minHourIndex) => (int)SumFloat(minHourIndex, b => b.workTicksSpent, d => d.workTicksSpent, q => q.workTicksSpent, y => y.workTicksSpent);

        public int SumPawnTravelTicks(int pawnId, int minHourIndex)
        {
            int sum = 0;
            foreach (HourlyWorkBucket bucket in hourly.Where(b => b.hourIndex >= minHourIndex))
            {
                if (bucket.pawnTravelTicksSpent.TryGetValue(pawnId, out int ticks))
                {
                    sum += ticks;
                }
            }

            return sum;
        }

        public int SumPawnEndlessJobs(int pawnId, int minHourIndex)
        {
            int sum = 0;
            foreach (HourlyWorkBucket bucket in hourly.Where(b => b.hourIndex >= minHourIndex))
            {
                if (bucket.pawnEndlessJobCount.TryGetValue(pawnId, out int count))
                {
                    sum += count;
                }
            }

            return sum;
        }

        public int SumPawnWorkTicks(int pawnId, int minHourIndex)
        {
            int sum = 0;
            foreach (HourlyWorkBucket bucket in hourly.Where(b => b.hourIndex >= minHourIndex))
            {
                if (bucket.pawnWorkTicksSpent.TryGetValue(pawnId, out int ticks))
                {
                    sum += ticks;
                }
            }

            return sum;
        }

        public void RollupIfBoundaryCrossed(long absTick, Vector2 longitude)
        {
            int rolloverHour = WorkMonitorUtility.DayRolloverHour();
            int currentDayId = WorkMonitorUtility.GetWorkDayId(absTick, longitude, rolloverHour);
            int year = GenDate.Year(absTick, longitude.x);
            int quadrum = (int)GenDate.Quadrum(absTick, longitude.x);
            int quadrumKey = year * 4 + quadrum;

            RollupCompletedDays(currentDayId, rolloverHour, longitude);
            RollupCompletedQuadrums(quadrumKey, longitude);
            RollupCompletedYears(year, longitude);
        }

        private void RollupCompletedDays(int currentDayId, int rolloverHour, Vector2 longitude)
        {
            int maxDaily = WorkMonitorMod.Settings?.maxDailyBuckets ?? 20;
            List<int> dayIds = hourly.Select(b => WorkMonitorUtility.GetWorkDayId(
                (long)b.hourIndex * WorkMonitorSettings.TicksPerHour + rolloverHour * GenDate.TicksPerHour,
                longitude,
                rolloverHour)).Distinct().Where(d => d < currentDayId).OrderBy(d => d).ToList();

            foreach (int dayId in dayIds)
            {
                if (daily.Any(d => d.dayId == dayId))
                {
                    hourly.RemoveAll(b => WorkMonitorUtility.GetWorkDayId(
                        (long)b.hourIndex * WorkMonitorSettings.TicksPerHour + rolloverHour * GenDate.TicksPerHour,
                        longitude,
                        rolloverHour) == dayId);
                    continue;
                }

                List<HourlyWorkBucket> dayBuckets = hourly.Where(b => WorkMonitorUtility.GetWorkDayId(
                    (long)b.hourIndex * WorkMonitorSettings.TicksPerHour + rolloverHour * GenDate.TicksPerHour,
                    longitude,
                    rolloverHour) == dayId).ToList();

                if (dayBuckets.Count == 0)
                {
                    continue;
                }

                DailyWorkBucket dailyBucket = new DailyWorkBucket
                {
                    dayId = dayId,
                    startHourIndex = dayBuckets.Min(b => b.hourIndex),
                    endHourIndex = dayBuckets.Max(b => b.hourIndex) + 1
                };

                foreach (HourlyWorkBucket h in dayBuckets)
                {
                    dailyBucket.MergeFrom(h);
                }

                daily.Add(dailyBucket);
                daily.Sort((a, b) => a.dayId.CompareTo(b.dayId));
                hourly.RemoveAll(b => dayBuckets.Contains(b));

                while (daily.Count > maxDaily)
                {
                    daily.RemoveAt(0);
                }
            }
        }

        private void RollupCompletedQuadrums(int currentQuadrumKey, Vector2 longitude)
        {
            int maxQuadrum = WorkMonitorMod.Settings?.maxQuadrumBuckets ?? 12;
            List<int> keys = daily.Select(d => QuadrumKeyForDay(d.dayId, longitude)).Distinct().Where(k => k < currentQuadrumKey).OrderBy(k => k).ToList();

            foreach (int key in keys)
            {
                if (quadrums.Any(q => q.quadrumKey == key))
                {
                    daily.RemoveAll(d => QuadrumKeyForDay(d.dayId, longitude) == key);
                    continue;
                }

                List<DailyWorkBucket> dayBuckets = daily.Where(d => QuadrumKeyForDay(d.dayId, longitude) == key).ToList();
                if (dayBuckets.Count == 0)
                {
                    continue;
                }

                QuadrumWorkBucket qBucket = new QuadrumWorkBucket { quadrumKey = key };
                foreach (DailyWorkBucket d in dayBuckets)
                {
                    qBucket.MergeFrom(d);
                }

                quadrums.Add(qBucket);
                quadrums.Sort((a, b) => a.quadrumKey.CompareTo(b.quadrumKey));
                daily.RemoveAll(d => dayBuckets.Contains(d));

                while (quadrums.Count > maxQuadrum)
                {
                    quadrums.RemoveAt(0);
                }
            }
        }

        private void RollupCompletedYears(int currentYear, Vector2 longitude)
        {
            bool unlimited = WorkMonitorMod.Settings?.yearHistoryUnlimited ?? false;
            int maxYear = WorkMonitorMod.Settings?.maxYearBuckets ?? 7;
            List<int> yearsToRoll = quadrums.Select(q => q.quadrumKey / 4).Distinct().Where(y => y < currentYear).OrderBy(y => y).ToList();

            foreach (int year in yearsToRoll)
            {
                if (years.Any(y => y.year == year))
                {
                    quadrums.RemoveAll(q => q.quadrumKey / 4 == year);
                    continue;
                }

                List<QuadrumWorkBucket> qBuckets = quadrums.Where(q => q.quadrumKey / 4 == year).ToList();
                if (qBuckets.Count == 0)
                {
                    continue;
                }

                YearWorkBucket yBucket = new YearWorkBucket { year = year };
                foreach (QuadrumWorkBucket q in qBuckets)
                {
                    yBucket.MergeFrom(q);
                }

                years.Add(yBucket);
                years.Sort((a, b) => a.year.CompareTo(b.year));
                quadrums.RemoveAll(q => qBuckets.Contains(q));

                if (!unlimited)
                {
                    while (years.Count > maxYear)
                    {
                        years.RemoveAt(0);
                    }
                }
            }
        }

        private static int QuadrumKeyForDay(int dayId, Vector2 longitude)
        {
            int hourIndex = WorkMonitorUtility.HourIndexForDayStart(dayId, WorkMonitorUtility.DayRolloverHour());
            long absTick = (long)hourIndex * WorkMonitorSettings.TicksPerHour;
            float lon = longitude.x;
            int year = GenDate.Year(absTick, lon);
            int quadrum = (int)GenDate.Quadrum(absTick, lon);
            return year * 4 + quadrum;
        }

        private float SumFloat(int minHourIndex,
            System.Func<HourlyWorkBucket, float> hourlySel,
            System.Func<DailyWorkBucket, float> dailySel,
            System.Func<QuadrumWorkBucket, float> quadrumSel,
            System.Func<YearWorkBucket, float> yearSel)
        {
            float sum = 0f;
            int currentHour = WorkMonitorUtility.CurrentHourIndex();
            int hourlyMin = Mathf.Max(minHourIndex, currentHour - WorkMonitorSettings.MaxRetentionHours);

            foreach (HourlyWorkBucket b in hourly.Where(b => b.hourIndex >= hourlyMin && b.hourIndex >= minHourIndex))
            {
                sum += hourlySel(b);
            }

            foreach (DailyWorkBucket d in daily.Where(d => d.endHourIndex > minHourIndex && d.startHourIndex < currentHour))
            {
                if (d.endHourIndex <= hourlyMin)
                {
                    sum += dailySel(d);
                }
            }

            if (minHourIndex < hourlyMin)
            {
                foreach (QuadrumWorkBucket q in quadrums)
                {
                    sum += quadrumSel(q);
                }

                foreach (YearWorkBucket y in years)
                {
                    sum += yearSel(y);
                }
            }

            return sum;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref maxHourlyHours, "maxHourlyHours", WorkMonitorSettings.MaxRetentionHours);
            Scribe_Collections.Look(ref hourly, "buckets", LookMode.Deep);
            Scribe_Collections.Look(ref daily, "daily", LookMode.Deep);
            Scribe_Collections.Look(ref quadrums, "quadrums", LookMode.Deep);
            Scribe_Collections.Look(ref years, "years", LookMode.Deep);
            hourly ??= new List<HourlyWorkBucket>();
            daily ??= new List<DailyWorkBucket>();
            quadrums ??= new List<QuadrumWorkBucket>();
            years ??= new List<YearWorkBucket>();
        }

        private void TrimHourly()
        {
            while (hourly.Count > maxHourlyHours)
            {
                hourly.RemoveAt(0);
            }
        }
    }
}
