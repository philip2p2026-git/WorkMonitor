using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using WorkMonitor.Tracking;
using WorkMonitor.UI;

namespace WorkMonitor.Groups
{
    public static class ColonistStatsAggregator
    {
        public static ColonistStats Build(Pawn pawn, int rangeHours, List<WorkGroupStats> allGroupStats = null)
        {
            if (pawn == null)
            {
                return null;
            }

            return Build(pawn.thingIDNumber, rangeHours, allGroupStats);
        }

        public static ColonistStats Build(int pawnId, int rangeHours, List<WorkGroupStats> allGroupStats = null)
        {
            if (pawnId <= 0)
            {
                return null;
            }

            allGroupStats ??= WorkGroupStatsAggregator.BuildAll(rangeHours);
            WorkActivityTracker tracker = WorkActivityTracker.EnsureRegistered();
            int minHour = WorkMonitorUtility.CurrentHourIndex() - rangeHours;
            Pawn pawn = ColonistWorkQuery.TryResolvePawn(pawnId);
            var stats = new ColonistStats
            {
                PawnId = pawnId,
                Pawn = pawn,
                Label = ColonistWorkQuery.ResolveLabel(pawnId, tracker),
                IsAbsent = ColonistWorkQuery.IsAbsent(pawnId, tracker)
            };

            Passion topPassion = Passion.None;
            foreach (WorkGroupStats groupStats in allGroupStats)
            {
                WorkGroupSnapshot group = groupStats.Group;
                int pawnJobs = 0;
                int pawnEndlessJobs = 0;
                int pawnTicks = 0;
                int pawnTravel = 0;
                int pawnWork = 0;
                float pawnWorkUnits = 0f;

                foreach (WorkGiverDef wg in group.WorkGivers)
                {
                    pawnJobs += tracker?.SumPawnWorkGiverJobs(pawnId, wg.defName, minHour) ?? 0;
                    pawnEndlessJobs += tracker?.SumPawnWorkGiverEndlessJobs(pawnId, wg.defName, minHour) ?? 0;
                    pawnTicks += tracker?.SumPawnWorkGiverTicks(pawnId, wg.defName, minHour) ?? 0;
                    pawnTravel += tracker?.SumPawnWorkGiverTravelTicks(pawnId, wg.defName, minHour) ?? 0;
                    pawnWork += tracker?.SumPawnWorkGiverWorkTicks(pawnId, wg.defName, minHour) ?? 0;
                    pawnWorkUnits += tracker?.SumPawnWorkGiverWorkUnits(pawnId, wg.defName, minHour) ?? 0f;
                }

                if (pawnJobs <= 0 && pawnEndlessJobs <= 0 && pawnTicks <= 0 && pawnWorkUnits <= 0f)
                {
                    continue;
                }

                Passion passion = ColonistWorkQuery.ResolvePassionForGroup(pawnId, group);
                if ((int)passion > (int)topPassion)
                {
                    topPassion = passion;
                }

                stats.GroupStats.Add(new ColonistGroupStat
                {
                    Group = group,
                    MapOpenTasks = groupStats.TotalMapOpenTasks,
                    MapWorkLeft = groupStats.TotalMapWorkLeft,
                    JobCount = pawnJobs,
                    EndlessJobCount = pawnEndlessJobs,
                    TicksSpent = pawnTicks,
                    TravelTicksSpent = pawnTravel,
                    WorkTicksSpent = pawnWork,
                    WorkUnitsSpent = pawnWorkUnits,
                    GroupJobCount = groupStats.TotalJobCount,
                    GroupWorkUnits = groupStats.TotalWorkUnits,
                    GroupTicksSpent = groupStats.TotalTicksSpent
                });

                stats.TotalJobCount += pawnJobs;
                stats.TotalEndlessJobCount += pawnEndlessJobs;
                stats.TotalTicksSpent += pawnTicks;
                stats.TotalTravelTicksSpent += pawnTravel;
                stats.TotalWorkTicksSpent += pawnWork;
                stats.TotalWorkUnits += pawnWorkUnits;
            }

            stats.TopPassion = topPassion;
            Dictionary<string, int> order = new Dictionary<string, int>();
            int index = 0;
            foreach (WorkGroupSnapshot group in WorkGroupRegistry.GetAllGroups())
            {
                order[group.Key.StorageKey] = index++;
            }

            stats.GroupStats = stats.GroupStats
                .OrderBy(g => order.TryGetValue(g.Group.Key.StorageKey, out int i) ? i : int.MaxValue)
                .ToList();
            return stats;
        }

        public static ColonistGroupWorkDetail BuildGroupDetail(Pawn pawn, WorkGroupSnapshot group, int rangeHours)
        {
            if (pawn == null)
            {
                return null;
            }

            return BuildGroupDetail(pawn.thingIDNumber, group, rangeHours);
        }

        public static ColonistGroupWorkDetail BuildGroupDetail(int pawnId, WorkGroupSnapshot group, int rangeHours)
        {
            if (pawnId <= 0 || group == null)
            {
                return null;
            }

            WorkActivityTracker tracker = WorkActivityTracker.EnsureRegistered();
            int minHour = WorkMonitorUtility.CurrentHourIndex() - rangeHours;
            var detail = new ColonistGroupWorkDetail
            {
                PawnId = pawnId,
                Pawn = ColonistWorkQuery.TryResolvePawn(pawnId),
                Group = group
            };

            foreach (WorkGiverDef wg in group.WorkGivers)
            {
                int jobs = tracker?.SumPawnWorkGiverJobs(pawnId, wg.defName, minHour) ?? 0;
                int endless = tracker?.SumPawnWorkGiverEndlessJobs(pawnId, wg.defName, minHour) ?? 0;
                int ticks = tracker?.SumPawnWorkGiverTicks(pawnId, wg.defName, minHour) ?? 0;
                int travel = tracker?.SumPawnWorkGiverTravelTicks(pawnId, wg.defName, minHour) ?? 0;
                int work = tracker?.SumPawnWorkGiverWorkTicks(pawnId, wg.defName, minHour) ?? 0;
                float units = tracker?.SumPawnWorkGiverWorkUnits(pawnId, wg.defName, minHour) ?? 0f;

                if (jobs <= 0 && endless <= 0 && ticks <= 0 && travel <= 0 && work <= 0 && units <= 0f)
                {
                    continue;
                }

                detail.WorkGiverStats.Add(new ColonistWorkGiverStat
                {
                    WorkGiver = wg,
                    Label = WorkGiverLabelUtility.Format(wg),
                    JobCount = jobs,
                    EndlessJobCount = endless,
                    TicksSpent = ticks,
                    TravelTicksSpent = travel,
                    WorkTicksSpent = work,
                    WorkUnitsSpent = units
                });

                detail.JobCount += jobs;
                detail.EndlessJobCount += endless;
                detail.TravelTicksSpent += travel;
                detail.WorkTicksSpent += work;
                detail.WorkUnitsSpent += units;
            }

            return detail;
        }
    }
}
