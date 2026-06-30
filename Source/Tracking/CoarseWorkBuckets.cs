using System.Collections.Generic;
using Verse;

namespace WorkMonitor.Tracking
{
    public class DailyWorkBucket : IExposable
    {
        public int dayId;
        public int startHourIndex;
        public int endHourIndex;
        public int jobCount;
        public int endlessJobCount;
        public int ticksSpent;
        public int travelTicksSpent;
        public int workTicksSpent;
        public float workUnitsSpent;
        public float estimatedWorkUnitsSpent;

        public void ExposeData()
        {
            Scribe_Values.Look(ref dayId, "dayId");
            Scribe_Values.Look(ref startHourIndex, "startHourIndex");
            Scribe_Values.Look(ref endHourIndex, "endHourIndex");
            Scribe_Values.Look(ref jobCount, "jobCount");
            Scribe_Values.Look(ref endlessJobCount, "endlessJobCount", 0);
            Scribe_Values.Look(ref ticksSpent, "ticksSpent");
            Scribe_Values.Look(ref travelTicksSpent, "travelTicksSpent", 0);
            Scribe_Values.Look(ref workTicksSpent, "workTicksSpent", 0);
            Scribe_Values.Look(ref workUnitsSpent, "workUnitsSpent", 0f);
            Scribe_Values.Look(ref estimatedWorkUnitsSpent, "estimatedWorkUnitsSpent", 0f);
        }

        public void MergeFrom(HourlyWorkBucket hourly)
        {
            jobCount += hourly.jobCount;
            endlessJobCount += hourly.endlessJobCount;
            ticksSpent += hourly.ticksSpent;
            travelTicksSpent += hourly.travelTicksSpent;
            workTicksSpent += hourly.workTicksSpent;
            workUnitsSpent += hourly.workUnitsSpent;
            estimatedWorkUnitsSpent += hourly.estimatedWorkUnitsSpent;
        }
    }

    public class QuadrumWorkBucket : IExposable
    {
        public int quadrumKey;
        public int jobCount;
        public int endlessJobCount;
        public int ticksSpent;
        public int travelTicksSpent;
        public int workTicksSpent;
        public float workUnitsSpent;
        public float estimatedWorkUnitsSpent;

        public void ExposeData()
        {
            Scribe_Values.Look(ref quadrumKey, "quadrumKey");
            Scribe_Values.Look(ref jobCount, "jobCount");
            Scribe_Values.Look(ref endlessJobCount, "endlessJobCount", 0);
            Scribe_Values.Look(ref ticksSpent, "ticksSpent");
            Scribe_Values.Look(ref travelTicksSpent, "travelTicksSpent", 0);
            Scribe_Values.Look(ref workTicksSpent, "workTicksSpent", 0);
            Scribe_Values.Look(ref workUnitsSpent, "workUnitsSpent", 0f);
            Scribe_Values.Look(ref estimatedWorkUnitsSpent, "estimatedWorkUnitsSpent", 0f);
        }

        public void MergeFrom(DailyWorkBucket daily)
        {
            jobCount += daily.jobCount;
            endlessJobCount += daily.endlessJobCount;
            ticksSpent += daily.ticksSpent;
            travelTicksSpent += daily.travelTicksSpent;
            workTicksSpent += daily.workTicksSpent;
            workUnitsSpent += daily.workUnitsSpent;
            estimatedWorkUnitsSpent += daily.estimatedWorkUnitsSpent;
        }
    }

    public class YearWorkBucket : IExposable
    {
        public int year;
        public int jobCount;
        public int endlessJobCount;
        public int ticksSpent;
        public int travelTicksSpent;
        public int workTicksSpent;
        public float workUnitsSpent;
        public float estimatedWorkUnitsSpent;

        public void ExposeData()
        {
            Scribe_Values.Look(ref year, "year");
            Scribe_Values.Look(ref jobCount, "jobCount");
            Scribe_Values.Look(ref endlessJobCount, "endlessJobCount", 0);
            Scribe_Values.Look(ref ticksSpent, "ticksSpent");
            Scribe_Values.Look(ref travelTicksSpent, "travelTicksSpent", 0);
            Scribe_Values.Look(ref workTicksSpent, "workTicksSpent", 0);
            Scribe_Values.Look(ref workUnitsSpent, "workUnitsSpent", 0f);
            Scribe_Values.Look(ref estimatedWorkUnitsSpent, "estimatedWorkUnitsSpent", 0f);
        }

        public void MergeFrom(QuadrumWorkBucket quadrum)
        {
            jobCount += quadrum.jobCount;
            endlessJobCount += quadrum.endlessJobCount;
            ticksSpent += quadrum.ticksSpent;
            travelTicksSpent += quadrum.travelTicksSpent;
            workTicksSpent += quadrum.workTicksSpent;
            workUnitsSpent += quadrum.workUnitsSpent;
            estimatedWorkUnitsSpent += quadrum.estimatedWorkUnitsSpent;
        }
    }
}
