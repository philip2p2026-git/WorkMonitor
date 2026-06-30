using System.Collections.Generic;
using Verse;

namespace WorkMonitor.Tracking
{
    public static class PawnBucketMergeUtility
    {
        public static void MergeIntDict(Dictionary<int, int> target, Dictionary<int, int> source)
        {
            if (source == null)
            {
                return;
            }

            foreach (KeyValuePair<int, int> entry in source)
            {
                if (!target.ContainsKey(entry.Key))
                {
                    target[entry.Key] = 0;
                }

                target[entry.Key] += entry.Value;
            }
        }

        public static void MergeFloatDict(Dictionary<int, float> target, Dictionary<int, float> source)
        {
            if (source == null)
            {
                return;
            }

            foreach (KeyValuePair<int, float> entry in source)
            {
                if (!target.ContainsKey(entry.Key))
                {
                    target[entry.Key] = 0f;
                }

                target[entry.Key] += entry.Value;
            }
        }

        public static int GetInt(Dictionary<int, int> dict, int pawnId)
        {
            return dict != null && dict.TryGetValue(pawnId, out int value) ? value : 0;
        }

        public static float GetFloat(Dictionary<int, float> dict, int pawnId)
        {
            return dict != null && dict.TryGetValue(pawnId, out float value) ? value : 0f;
        }

        public static void ExposeIntDict(ref Dictionary<int, int> dict, string label)
        {
            Scribe_Collections.Look(ref dict, label, LookMode.Value, LookMode.Value);
            dict ??= new Dictionary<int, int>();
        }

        public static void ExposeFloatDict(ref Dictionary<int, float> dict, string label)
        {
            Scribe_Collections.Look(ref dict, label, LookMode.Value, LookMode.Value);
            dict ??= new Dictionary<int, float>();
        }

        public static void MergePawnFields(PawnWorkBucketFields target, PawnWorkBucketFields source)
        {
            if (source == null)
            {
                return;
            }

            MergeIntDict(target.pawnTicksSpent, source.pawnTicksSpent);
            MergeIntDict(target.pawnTravelTicksSpent, source.pawnTravelTicksSpent);
            MergeIntDict(target.pawnWorkTicksSpent, source.pawnWorkTicksSpent);
            MergeFloatDict(target.pawnWorkUnitsSpent, source.pawnWorkUnitsSpent);
            MergeIntDict(target.pawnJobCount, source.pawnJobCount);
            MergeIntDict(target.pawnEndlessJobCount, source.pawnEndlessJobCount);
        }
    }

    public class PawnWorkBucketFields
    {
        public Dictionary<int, int> pawnTicksSpent = new Dictionary<int, int>();
        public Dictionary<int, int> pawnTravelTicksSpent = new Dictionary<int, int>();
        public Dictionary<int, int> pawnWorkTicksSpent = new Dictionary<int, int>();
        public Dictionary<int, float> pawnWorkUnitsSpent = new Dictionary<int, float>();
        public Dictionary<int, int> pawnJobCount = new Dictionary<int, int>();
        public Dictionary<int, int> pawnEndlessJobCount = new Dictionary<int, int>();

        public void ExposePawnFields(string prefix)
        {
            PawnBucketMergeUtility.ExposeIntDict(ref pawnTicksSpent, prefix + ".pawnTicksSpent");
            PawnBucketMergeUtility.ExposeIntDict(ref pawnTravelTicksSpent, prefix + ".pawnTravelTicksSpent");
            PawnBucketMergeUtility.ExposeIntDict(ref pawnWorkTicksSpent, prefix + ".pawnWorkTicksSpent");
            PawnBucketMergeUtility.ExposeFloatDict(ref pawnWorkUnitsSpent, prefix + ".pawnWorkUnitsSpent");
            PawnBucketMergeUtility.ExposeIntDict(ref pawnJobCount, prefix + ".pawnJobCount");
            PawnBucketMergeUtility.ExposeIntDict(ref pawnEndlessJobCount, prefix + ".pawnEndlessJobCount");
        }

        public void MergeFromHourly(HourlyWorkBucket hourly)
        {
            PawnBucketMergeUtility.MergeIntDict(pawnTicksSpent, hourly.pawnTicksSpent);
            PawnBucketMergeUtility.MergeIntDict(pawnTravelTicksSpent, hourly.pawnTravelTicksSpent);
            PawnBucketMergeUtility.MergeIntDict(pawnWorkTicksSpent, hourly.pawnWorkTicksSpent);
            PawnBucketMergeUtility.MergeFloatDict(pawnWorkUnitsSpent, hourly.pawnWorkUnitsSpent);
            PawnBucketMergeUtility.MergeIntDict(pawnJobCount, hourly.pawnJobCount);
            PawnBucketMergeUtility.MergeIntDict(pawnEndlessJobCount, hourly.pawnEndlessJobCount);
        }

        public void MergeFromFields(PawnWorkBucketFields other)
        {
            PawnBucketMergeUtility.MergePawnFields(this, other);
        }
    }
}
