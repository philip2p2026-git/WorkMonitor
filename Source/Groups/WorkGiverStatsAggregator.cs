using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using WorkMonitor.Tracking;
using WorkMonitor.UI;

namespace WorkMonitor.Groups
{
    public static class WorkGiverStatsAggregator
    {
        public static WorkGiverDetailStats Build(WorkGroupSnapshot group, WorkGiverDef workGiver, int rangeHours)
        {
            if (group == null || workGiver == null)
            {
                return null;
            }

            WorkActivityTracker tracker = WorkActivityTracker.EnsureRegistered();
            int minHour = WorkMonitorUtility.CurrentHourIndex() - rangeHours;
            MapWorkSnapshot mapSnapshot = MapWorkSampler.EnsureRegistered()?.GetLatestSnapshot();

            var detail = new WorkGiverDetailStats
            {
                Group = group,
                WorkGiver = workGiver,
                Label = WorkGiverLabelUtility.Format(workGiver)
            };

            foreach (Pawn pawn in WorkMonitorUtility.MonitorColonists())
            {
                int jobs = tracker?.SumPawnWorkGiverJobs(pawn.thingIDNumber, workGiver.defName, minHour) ?? 0;
                int endless = tracker?.SumPawnWorkGiverEndlessJobs(pawn.thingIDNumber, workGiver.defName, minHour) ?? 0;
                int travel = tracker?.SumPawnWorkGiverTravelTicks(pawn.thingIDNumber, workGiver.defName, minHour) ?? 0;
                int work = tracker?.SumPawnWorkGiverWorkTicks(pawn.thingIDNumber, workGiver.defName, minHour) ?? 0;
                float units = tracker?.SumPawnWorkGiverWorkUnits(pawn.thingIDNumber, workGiver.defName, minHour) ?? 0f;
                int ticks = tracker?.SumPawnWorkGiverTicks(pawn.thingIDNumber, workGiver.defName, minHour) ?? 0;

                if (jobs <= 0 && endless <= 0 && ticks <= 0 && units <= 0f)
                {
                    continue;
                }

                detail.TotalJobCount += jobs;
                detail.TotalEndlessJobCount += endless;
                detail.TotalTravelTicks += travel;
                detail.TotalWorkTicks += work;
                detail.TotalWorkUnits += units;

                Passion passion = Passion.None;
                if (workGiver.workType != null)
                {
                    passion = pawn.skills.MaxPassionOfRelevantSkillsFor(workGiver.workType);
                }

                var colonistStat = new ColonistWorkStat
                {
                    Pawn = pawn,
                    Label = pawn.LabelShort,
                    JobCount = jobs,
                    EndlessJobCount = endless,
                    TicksSpent = ticks,
                    TravelTicksSpent = travel,
                    WorkTicksSpent = work,
                    WorkUnitsSpent = units,
                    Passion = passion
                };

                float hours = ticks / (float)WorkMonitorSettings.TicksPerHour;
                if (hours > 0f)
                {
                    colonistStat.JobsPerHour = jobs / hours;
                    colonistStat.WorkUnitsPerHour = units / hours;
                }

                detail.ColonistStats.Add(colonistStat);
            }

            if (mapSnapshot != null)
            {
                detail.MapSampleTick = mapSnapshot.sampleTick;
                if (mapSnapshot.perWorkGiver.TryGetValue(workGiver.defName, out MapWorkGiverSnapshot mapSnap))
                {
                    detail.MapOpenTasks = mapSnap.openTaskCount;
                    detail.MapNewTodayOpenTasks = mapSnap.newTodayOpenTaskCount;
                    detail.MapWorkLeft = mapSnap.workLeftTotal;
                    detail.MapNewTodayWorkLeft = mapSnap.newTodayWorkLeftTotal;
                }
            }

            detail.ColonistStats = detail.ColonistStats.OrderByDescending(c => c.TicksSpent).ToList();
            return detail;
        }
    }
}
