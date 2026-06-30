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

        public bool HasAnyRetainedData()
        {
            return hourly.Count > 0 || daily.Count > 0 || quadrums.Count > 0 || years.Count > 0;
        }

        public void Configure(int hourlyRetentionHours)
        {
            maxHourlyHours = Mathf.Clamp(hourlyRetentionHours, 6, WorkMonitorSettings.MaxRetentionHours);
            TrimHourly();
        }

        public void ConfigurePawnHistory()
        {
            maxHourlyHours = WorkMonitorSettings.MaxRetentionHours;
            TrimHourly();
        }

        public float EstimateHourlyFromDaily(int hourIndex, System.Func<HourlyWorkBucket, float> hourlySelector, System.Func<DailyWorkBucket, float> dailySelector)
        {
            HourlyWorkBucket bucket = GetBucket(hourIndex);
            if (bucket != null)
            {
                return hourlySelector(bucket);
            }

            foreach (DailyWorkBucket day in daily)
            {
                if (hourIndex < day.startHourIndex || hourIndex >= day.endHourIndex)
                {
                    continue;
                }

                int span = Mathf.Max(1, day.endHourIndex - day.startHourIndex);
                return dailySelector(day) / span;
            }

            return 0f;
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

        public void PruneHourlyRetention()
        {
            int minHour = WorkMonitorUtility.CurrentHourIndex() - WorkMonitorSettings.MaxRetentionHours;
            hourly.RemoveAll(b => b.hourIndex < minHour);
            TrimHourly();
        }

        public int SumJobCount(int minHourIndex) => (int)SumFloat(minHourIndex, b => b.jobCount, d => d.jobCount, q => q.jobCount, y => y.jobCount);
        public int SumEndlessJobCount(int minHourIndex) => (int)SumFloat(minHourIndex, b => b.endlessJobCount, d => d.endlessJobCount, q => q.endlessJobCount, y => y.endlessJobCount);
        public int SumTicksSpent(int minHourIndex) => (int)SumFloat(minHourIndex, b => b.ticksSpent, d => d.ticksSpent, q => q.ticksSpent, y => y.ticksSpent);
        public float SumWorkUnits(int minHourIndex) => SumFloat(minHourIndex, b => b.workUnitsSpent + b.estimatedWorkUnitsSpent, d => d.workUnitsSpent + d.estimatedWorkUnitsSpent, q => q.workUnitsSpent + q.estimatedWorkUnitsSpent, y => y.workUnitsSpent + y.estimatedWorkUnitsSpent);
        public int SumTravelTicksSpent(int minHourIndex) => (int)SumFloat(minHourIndex, b => b.travelTicksSpent, d => d.travelTicksSpent, q => q.travelTicksSpent, y => y.travelTicksSpent);
        public int SumWorkTicksSpent(int minHourIndex) => (int)SumFloat(minHourIndex, b => b.workTicksSpent, d => d.workTicksSpent, q => q.workTicksSpent, y => y.workTicksSpent);

        public int SumPawnTravelTicks(int pawnId, int minHourIndex) =>
            (int)SumPawnFloat(pawnId, minHourIndex,
                b => PawnBucketMergeUtility.GetInt(b.pawnTravelTicksSpent, pawnId),
                d => PawnBucketMergeUtility.GetInt(d.pawnFields.pawnTravelTicksSpent, pawnId),
                q => PawnBucketMergeUtility.GetInt(q.pawnFields.pawnTravelTicksSpent, pawnId),
                y => PawnBucketMergeUtility.GetInt(y.pawnFields.pawnTravelTicksSpent, pawnId));

        public int SumPawnEndlessJobs(int pawnId, int minHourIndex) =>
            (int)SumPawnFloat(pawnId, minHourIndex,
                b => PawnBucketMergeUtility.GetInt(b.pawnEndlessJobCount, pawnId),
                d => PawnBucketMergeUtility.GetInt(d.pawnFields.pawnEndlessJobCount, pawnId),
                q => PawnBucketMergeUtility.GetInt(q.pawnFields.pawnEndlessJobCount, pawnId),
                y => PawnBucketMergeUtility.GetInt(y.pawnFields.pawnEndlessJobCount, pawnId));

        public int SumPawnWorkTicks(int pawnId, int minHourIndex) =>
            (int)SumPawnFloat(pawnId, minHourIndex,
                b => PawnBucketMergeUtility.GetInt(b.pawnWorkTicksSpent, pawnId),
                d => PawnBucketMergeUtility.GetInt(d.pawnFields.pawnWorkTicksSpent, pawnId),
                q => PawnBucketMergeUtility.GetInt(q.pawnFields.pawnWorkTicksSpent, pawnId),
                y => PawnBucketMergeUtility.GetInt(y.pawnFields.pawnWorkTicksSpent, pawnId));

        public int SumPawnJobCount(int pawnId, int minHourIndex) =>
            (int)SumPawnFloat(pawnId, minHourIndex,
                b => PawnBucketMergeUtility.GetInt(b.pawnJobCount, pawnId),
                d => PawnBucketMergeUtility.GetInt(d.pawnFields.pawnJobCount, pawnId),
                q => PawnBucketMergeUtility.GetInt(q.pawnFields.pawnJobCount, pawnId),
                y => PawnBucketMergeUtility.GetInt(y.pawnFields.pawnJobCount, pawnId));

        public float SumPawnWorkUnits(int pawnId, int minHourIndex) =>
            SumPawnFloat(pawnId, minHourIndex,
                b => PawnBucketMergeUtility.GetFloat(b.pawnWorkUnitsSpent, pawnId),
                d => PawnBucketMergeUtility.GetFloat(d.pawnFields.pawnWorkUnitsSpent, pawnId),
                q => PawnBucketMergeUtility.GetFloat(q.pawnFields.pawnWorkUnitsSpent, pawnId),
                y => PawnBucketMergeUtility.GetFloat(y.pawnFields.pawnWorkUnitsSpent, pawnId));

        public int SumPawnTicks(int pawnId, int minHourIndex) =>
            (int)SumPawnFloat(pawnId, minHourIndex,
                b => PawnBucketMergeUtility.GetInt(b.pawnTicksSpent, pawnId),
                d => PawnBucketMergeUtility.GetInt(d.pawnFields.pawnTicksSpent, pawnId),
                q => PawnBucketMergeUtility.GetInt(q.pawnFields.pawnTicksSpent, pawnId),
                y => PawnBucketMergeUtility.GetInt(y.pawnFields.pawnTicksSpent, pawnId));

        public bool PawnHasWorkInRange(int pawnId, int minHourIndex)
        {
            return SumPawnTicks(pawnId, minHourIndex) > 0
                || SumPawnJobCount(pawnId, minHourIndex) > 0
                || SumPawnEndlessJobs(pawnId, minHourIndex) > 0
                || SumPawnWorkUnits(pawnId, minHourIndex) > 0f;
        }

        public void CollectPawnIdsWithWork(int minHourIndex, HashSet<int> results)
        {
            if (results == null)
            {
                return;
            }

            int currentHour = WorkMonitorUtility.CurrentHourIndex();
            int hourlyMin = Mathf.Max(minHourIndex, currentHour - WorkMonitorSettings.MaxRetentionHours);

            foreach (HourlyWorkBucket bucket in hourly.Where(b => b.hourIndex >= hourlyMin && b.hourIndex >= minHourIndex))
            {
                CollectPawnIdsFromHourly(bucket, results);
            }

            foreach (DailyWorkBucket day in daily.Where(d => d.endHourIndex > minHourIndex && d.startHourIndex < currentHour))
            {
                if (day.endHourIndex <= hourlyMin)
                {
                    CollectPawnIdsFromFields(day.pawnFields, results);
                }
            }

            if (minHourIndex < hourlyMin)
            {
                foreach (QuadrumWorkBucket q in quadrums)
                {
                    CollectPawnIdsFromFields(q.pawnFields, results);
                }

                foreach (YearWorkBucket y in years)
                {
                    CollectPawnIdsFromFields(y.pawnFields, results);
                }
            }
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
            List<int> dayIds = hourly
                .Select(b => WorkMonitorUtility.GetWorkDayIdForHourIndex(b.hourIndex))
                .Distinct()
                .Where(d => d < currentDayId)
                .OrderBy(d => d)
                .ToList();

            foreach (int dayId in dayIds)
            {
                List<HourlyWorkBucket> dayBuckets = hourly
                    .Where(b => WorkMonitorUtility.GetWorkDayIdForHourIndex(b.hourIndex) == dayId)
                    .ToList();

                if (dayBuckets.Count == 0)
                {
                    continue;
                }

                DailyWorkBucket dailyBucket = daily.FirstOrDefault(d => d.dayId == dayId);
                if (dailyBucket == null)
                {
                    dailyBucket = new DailyWorkBucket { dayId = dayId };
                    daily.Add(dailyBucket);
                }

                dailyBucket.startHourIndex = dayBuckets.Min(b => b.hourIndex);
                dailyBucket.endHourIndex = dayBuckets.Max(b => b.hourIndex) + 1;
                dailyBucket.jobCount = 0;
                dailyBucket.endlessJobCount = 0;
                dailyBucket.ticksSpent = 0;
                dailyBucket.travelTicksSpent = 0;
                dailyBucket.workTicksSpent = 0;
                dailyBucket.workUnitsSpent = 0f;
                dailyBucket.estimatedWorkUnitsSpent = 0f;
                dailyBucket.pawnFields = new PawnWorkBucketFields();

                foreach (HourlyWorkBucket h in dayBuckets)
                {
                    dailyBucket.MergeFrom(h);
                }

                daily.Sort((a, b) => a.dayId.CompareTo(b.dayId));

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
                List<DailyWorkBucket> dayBuckets = daily.Where(d => QuadrumKeyForDay(d.dayId, longitude) == key).ToList();
                if (dayBuckets.Count == 0)
                {
                    continue;
                }

                QuadrumWorkBucket qBucket = quadrums.FirstOrDefault(q => q.quadrumKey == key);
                if (qBucket == null)
                {
                    qBucket = new QuadrumWorkBucket { quadrumKey = key };
                    quadrums.Add(qBucket);
                }

                qBucket.jobCount = 0;
                qBucket.endlessJobCount = 0;
                qBucket.ticksSpent = 0;
                qBucket.travelTicksSpent = 0;
                qBucket.workTicksSpent = 0;
                qBucket.workUnitsSpent = 0f;
                qBucket.estimatedWorkUnitsSpent = 0f;
                qBucket.pawnFields = new PawnWorkBucketFields();

                foreach (DailyWorkBucket d in dayBuckets)
                {
                    qBucket.MergeFrom(d);
                }

                quadrums.Sort((a, b) => a.quadrumKey.CompareTo(b.quadrumKey));

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
                List<QuadrumWorkBucket> qBuckets = quadrums.Where(q => q.quadrumKey / 4 == year).ToList();
                if (qBuckets.Count == 0)
                {
                    continue;
                }

                YearWorkBucket yBucket = years.FirstOrDefault(y => y.year == year);
                if (yBucket == null)
                {
                    yBucket = new YearWorkBucket { year = year };
                    years.Add(yBucket);
                }

                yBucket.jobCount = 0;
                yBucket.endlessJobCount = 0;
                yBucket.ticksSpent = 0;
                yBucket.travelTicksSpent = 0;
                yBucket.workTicksSpent = 0;
                yBucket.workUnitsSpent = 0f;
                yBucket.estimatedWorkUnitsSpent = 0f;
                yBucket.pawnFields = new PawnWorkBucketFields();

                foreach (QuadrumWorkBucket q in qBuckets)
                {
                    yBucket.MergeFrom(q);
                }

                years.Sort((a, b) => a.year.CompareTo(b.year));

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

        private float SumPawnFloat(int pawnId, int minHourIndex,
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

        private static void CollectPawnIdsFromHourly(HourlyWorkBucket bucket, HashSet<int> results)
        {
            CollectPawnIdsFromDict(bucket.pawnTicksSpent, results);
            CollectPawnIdsFromDict(bucket.pawnJobCount, results);
            CollectPawnIdsFromDict(bucket.pawnEndlessJobCount, results);
            CollectPawnIdsFromDict(bucket.pawnWorkTicksSpent, results);
            if (bucket.pawnWorkUnitsSpent != null)
            {
                foreach (int pawnId in bucket.pawnWorkUnitsSpent.Keys)
                {
                    if (bucket.pawnWorkUnitsSpent[pawnId] > 0f)
                    {
                        results.Add(pawnId);
                    }
                }
            }
        }

        private static void CollectPawnIdsFromFields(PawnWorkBucketFields fields, HashSet<int> results)
        {
            if (fields == null)
            {
                return;
            }

            CollectPawnIdsFromDict(fields.pawnTicksSpent, results);
            CollectPawnIdsFromDict(fields.pawnJobCount, results);
            CollectPawnIdsFromDict(fields.pawnEndlessJobCount, results);
            CollectPawnIdsFromDict(fields.pawnWorkTicksSpent, results);
            if (fields.pawnWorkUnitsSpent != null)
            {
                foreach (int pawnId in fields.pawnWorkUnitsSpent.Keys)
                {
                    if (fields.pawnWorkUnitsSpent[pawnId] > 0f)
                    {
                        results.Add(pawnId);
                    }
                }
            }
        }

        private static void CollectPawnIdsFromDict(Dictionary<int, int> dict, HashSet<int> results)
        {
            if (dict == null)
            {
                return;
            }

            foreach (KeyValuePair<int, int> entry in dict)
            {
                if (entry.Value > 0)
                {
                    results.Add(entry.Key);
                }
            }
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
