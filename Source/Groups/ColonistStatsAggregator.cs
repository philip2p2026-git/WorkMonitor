using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using WorkMonitor.Groups;
using WorkMonitor.Tracking;
using WorkMonitor.UI;

namespace WorkMonitor.Groups
{
    public static class ColonistStatsAggregator
    {
        public static ColonistStats Build(Pawn pawn, List<WorkGroupStats> allGroupStats = null)
        {
            if (pawn == null)
            {
                return null;
            }

            allGroupStats ??= WorkGroupStatsAggregator.BuildAll();
            WorkActivityTracker tracker = WorkActivityTracker.EnsureRegistered();
            int minHour = WorkMonitorUtility.CurrentHourIndex() - (WorkMonitorMod.Settings?.statsWindowHours ?? 24);
            var stats = new ColonistStats
            {
                Pawn = pawn,
                Label = pawn.LabelShort
            };

            Passion topPassion = Passion.None;
            foreach (WorkGroupStats groupStats in allGroupStats)
            {
                WorkGroupSnapshot group = groupStats.Group;
                int pawnJobs = 0;
                int pawnTicks = 0;
                int pawnTravel = 0;
                int pawnWork = 0;
                float pawnWorkUnits = 0f;

                foreach (WorkGiverDef wg in group.WorkGivers)
                {
                    pawnJobs += tracker?.SumPawnWorkGiverJobs(pawn.thingIDNumber, wg.defName, minHour) ?? 0;
                    pawnTicks += tracker?.SumPawnWorkGiverTicks(pawn.thingIDNumber, wg.defName, minHour) ?? 0;
                    pawnTravel += tracker?.SumPawnWorkGiverTravelTicks(pawn.thingIDNumber, wg.defName, minHour) ?? 0;
                    pawnWork += tracker?.SumPawnWorkGiverWorkTicks(pawn.thingIDNumber, wg.defName, minHour) ?? 0;
                    pawnWorkUnits += tracker?.SumPawnWorkGiverWorkUnits(pawn.thingIDNumber, wg.defName, minHour) ?? 0f;
                }

                if (pawnJobs <= 0 && pawnTicks <= 0 && pawnWorkUnits <= 0f)
                {
                    continue;
                }

                Passion passion = Passion.None;
                foreach (WorkTypeDef workType in group.UniqueWorkTypes)
                {
                    if (workType == null)
                    {
                        continue;
                    }

                    Passion p = pawn.skills.MaxPassionOfRelevantSkillsFor(workType);
                    if ((int)p > (int)passion)
                    {
                        passion = p;
                    }
                }

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
                    TicksSpent = pawnTicks,
                    TravelTicksSpent = pawnTravel,
                    WorkTicksSpent = pawnWork,
                    WorkUnitsSpent = pawnWorkUnits,
                    GroupJobCount = groupStats.TotalJobCount,
                    GroupWorkUnits = groupStats.TotalWorkUnits,
                    GroupTicksSpent = groupStats.TotalTicksSpent
                });

                stats.TotalJobCount += pawnJobs;
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

        public static ColonistGroupWorkDetail BuildGroupDetail(Pawn pawn, WorkGroupSnapshot group)
        {
            if (pawn == null || group == null)
            {
                return null;
            }

            WorkActivityTracker tracker = WorkActivityTracker.EnsureRegistered();
            int minHour = WorkMonitorUtility.CurrentHourIndex() - (WorkMonitorMod.Settings?.statsWindowHours ?? 24);
            var detail = new ColonistGroupWorkDetail
            {
                Pawn = pawn,
                Group = group
            };

            foreach (WorkGiverDef wg in group.WorkGivers)
            {
                int jobs = tracker?.SumPawnWorkGiverJobs(pawn.thingIDNumber, wg.defName, minHour) ?? 0;
                int travel = tracker?.SumPawnWorkGiverTravelTicks(pawn.thingIDNumber, wg.defName, minHour) ?? 0;
                int work = tracker?.SumPawnWorkGiverWorkTicks(pawn.thingIDNumber, wg.defName, minHour) ?? 0;
                float units = tracker?.SumPawnWorkGiverWorkUnits(pawn.thingIDNumber, wg.defName, minHour) ?? 0f;

                if (jobs <= 0 && travel <= 0 && work <= 0 && units <= 0f)
                {
                    continue;
                }

                detail.WorkGiverStats.Add(new ColonistWorkGiverStat
                {
                    WorkGiver = wg,
                    Label = WorkGiverLabelUtility.Format(wg),
                    JobCount = jobs,
                    TravelTicksSpent = travel,
                    WorkTicksSpent = work,
                    WorkUnitsSpent = units
                });

                detail.JobCount += jobs;
                detail.TravelTicksSpent += travel;
                detail.WorkTicksSpent += work;
                detail.WorkUnitsSpent += units;
            }

            return detail;
        }
    }
}
