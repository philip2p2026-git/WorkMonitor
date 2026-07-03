using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using WorkMonitor.Tracking;
using WorkMonitor.UI;
using WorkTab;

namespace WorkMonitor.Groups
{
    public static class WorkGroupRegistry
    {
        private static List<WorkGroupSnapshot> cachedGroups;
        private static int lastCacheTick = -1;

        public static List<WorkGroupSnapshot> GetAllGroups(bool forceRefresh = false)
        {
            int tick = Find.TickManager.TicksGame;
            if (!forceRefresh && cachedGroups != null && tick - lastCacheTick < 250)
            {
                return cachedGroups;
            }

            WorkGiverAssignmentIndex.Rebuild(tick);

            List<WorkGroupSnapshot> groups = new List<WorkGroupSnapshot>();
            groups.AddRange(new WorkTypeGroupProvider().GetGroups());
            groups.AddRange(WorkTabGroupsProvider.GetCustomGroups());
            groups.AddRange(new OtherWorkGroupProvider().GetGroups());

            cachedGroups = WorkGroupOrderUtility.Sort(groups);
            lastCacheTick = tick;
            return cachedGroups;
        }
    }

    public static class WorkGroupStatsAggregator
    {
        public static WorkGroupStats Build(WorkGroupSnapshot group, int rangeHours)
        {
            WorkGroupStats stats = new WorkGroupStats { Group = group };
            List<Pawn> colonists = WorkMonitorUtility.MonitorColonists().ToList();
            WorkActivityTracker tracker = WorkActivityTracker.EnsureRegistered();
            int nowTick = WorkMonitorUtility.CurrentTicksGame();
            int minHour = WorkMonitorUtility.CurrentHourIndex() - rangeHours;
            WorkHistoryTierBuffer history = tracker?.GetGroupHistory(group.Key.StorageKey);
            MapWorkSnapshot mapSnapshot = MapWorkSampler.EnsureRegistered()?.GetLatestSnapshot();

            int mostRecentEnabledWorkTick = -1;

            foreach (Pawn pawn in colonists)
            {
                bool capable = IsCapable(pawn, group);
                bool enabled = IsEnabled(pawn, group);

                if (capable)
                {
                    stats.CapableCount++;
                    Passion passion = GetPassionForGroup(pawn, group);
                    if (passion == Passion.Major)
                    {
                        stats.MajorInterestCount++;
                    }
                    else if (passion == Passion.Minor)
                    {
                        stats.MinorInterestCount++;
                    }
                }

                if (enabled)
                {
                    stats.EnabledCount++;
                }

                if (!enabled)
                {
                    continue;
                }

                int pawnLastTick = -1;
                foreach (WorkGiverDef wg in group.WorkGivers)
                {
                    WorkActivityRecord record = tracker?.GetRecord(pawn.thingIDNumber, wg.defName);
                    if (record != null)
                    {
                        pawnLastTick = Mathf.Max(pawnLastTick, record.lastWorkTick);
                    }
                }

                if (pawnLastTick > mostRecentEnabledWorkTick)
                {
                    mostRecentEnabledWorkTick = pawnLastTick;
                }
            }

            List<int> colonistIds = ColonistWorkQuery.GetColonistIdsForGroup(group, minHour);
            foreach (int pawnId in colonistIds)
            {
                int pawnJobs = 0;
                int pawnEndlessJobs = 0;
                int pawnTicks = 0;
                int pawnTravel = 0;
                int pawnWork = 0;
                float pawnWorkUnits = 0f;

                foreach (WorkGiverDef wg in group.WorkGivers)
                {
                    int wgJobs = tracker?.SumPawnWorkGiverJobs(pawnId, wg.defName, minHour) ?? 0;
                    int wgEndless = tracker?.SumPawnWorkGiverEndlessJobs(pawnId, wg.defName, minHour) ?? 0;
                    int wgTicks = tracker?.SumPawnWorkGiverTicks(pawnId, wg.defName, minHour) ?? 0;
                    int wgTravel = tracker?.SumPawnWorkGiverTravelTicks(pawnId, wg.defName, minHour) ?? 0;
                    int wgWork = tracker?.SumPawnWorkGiverWorkTicks(pawnId, wg.defName, minHour) ?? 0;
                    float wgUnits = tracker?.SumPawnWorkGiverWorkUnits(pawnId, wg.defName, minHour) ?? 0f;
                    pawnJobs += wgJobs;
                    pawnEndlessJobs += wgEndless;
                    pawnTicks += wgTicks;
                    pawnTravel += wgTravel;
                    pawnWork += wgWork;
                    pawnWorkUnits += wgUnits;
                }

                if (pawnJobs <= 0 && pawnEndlessJobs <= 0 && pawnTicks <= 0)
                {
                    continue;
                }

                stats.WorkedCount++;

                Pawn pawn = ColonistWorkQuery.TryResolvePawn(pawnId);
                bool isAbsent = ColonistWorkQuery.IsAbsent(pawnId, tracker);
                var colonistStat = new ColonistWorkStat
                {
                    PawnId = pawnId,
                    Pawn = pawn,
                    Label = ColonistWorkQuery.ResolveLabel(pawnId, tracker),
                    IsAbsent = isAbsent,
                    JobCount = pawnJobs,
                    EndlessJobCount = pawnEndlessJobs,
                    TicksSpent = pawnTicks,
                    TravelTicksSpent = pawnTravel,
                    WorkTicksSpent = pawnWork,
                    WorkUnitsSpent = pawnWorkUnits,
                    Passion = ColonistWorkQuery.ResolvePassionForGroup(pawnId, group)
                };
                stats.ColonistStats.Add(colonistStat);
            }

            if (history != null)
            {
                stats.TotalJobCount = history.SumJobCount(minHour);
                stats.TotalEndlessJobCount = history.SumEndlessJobCount(minHour);
                stats.TotalTicksSpent = history.SumTicksSpent(minHour);
                stats.TotalWorkUnits = history.SumWorkUnits(minHour);
            }

            foreach (WorkGiverDef wg in group.WorkGivers)
            {
                MapWorkGiverSnapshot mapSnap = null;
                if (mapSnapshot?.perWorkGiver != null)
                {
                    mapSnapshot.perWorkGiver.TryGetValue(wg.defName, out mapSnap);
                }

                int wgJobs = 0;
                int wgEndless = 0;
                foreach (int pawnId in colonistIds)
                {
                    wgJobs += tracker?.SumPawnWorkGiverJobs(pawnId, wg.defName, minHour) ?? 0;
                    wgEndless += tracker?.SumPawnWorkGiverEndlessJobs(pawnId, wg.defName, minHour) ?? 0;
                }

                stats.WorkGiverStats.Add(new WorkGiverStat
                {
                    WorkGiver = wg,
                    Label = WorkGiverLabelUtility.Format(wg),
                    JobCount = wgJobs,
                    EndlessJobCount = wgEndless,
                    MapOpenTasks = mapSnap?.openTaskCount ?? 0,
                    MapNewTodayOpenTasks = mapSnap?.newTodayOpenTaskCount ?? 0,
                    MapWorkLeft = mapSnap?.workLeftTotal ?? 0f,
                    MapNewTodayWorkLeft = mapSnap?.newTodayWorkLeftTotal ?? 0f
                });
            }

            if (mapSnapshot != null)
            {
                stats.MapSampleTick = mapSnapshot.sampleTick;
                if (mapSnapshot.perGroupKey.TryGetValue(group.Key.StorageKey, out MapWorkGroupSnapshot groupSnap))
                {
                    stats.TotalMapOpenTasks = groupSnap.openTaskCount;
                    stats.TotalMapWorkLeft = groupSnap.workLeftTotal;
                    stats.TotalMapNewTodayOpenTasks = groupSnap.newTodayOpenTaskCount;
                    stats.TotalMapNewTodayWorkLeft = groupSnap.newTodayWorkLeftTotal;
                }
            }

            foreach (ColonistWorkStat colonist in stats.ColonistStats)
            {
                float hours = colonist.TicksSpent / (float)WorkMonitorSettings.TicksPerHour;
                if (hours > 0f)
                {
                    colonist.JobsPerHour = colonist.JobCount / hours;
                    colonist.WorkUnitsPerHour = colonist.WorkUnitsSpent / hours;
                }
            }

            stats.ColonistStats = stats.ColonistStats.OrderByDescending(c => c.TicksSpent).ToList();
            stats.Status = ResolveStatus(stats, mostRecentEnabledWorkTick, nowTick);
            return stats;
        }

        public static List<WorkGroupStats> BuildAll(int rangeHours)
        {
            return WorkGroupRegistry.GetAllGroups().Select(g => Build(g, rangeHours)).ToList();
        }

        private static bool IsCapable(Pawn pawn, WorkGroupSnapshot group)
        {
            return group.WorkGivers.Any(wg => pawn.CapableOf(wg));
        }

        private static bool IsEnabled(Pawn pawn, WorkGroupSnapshot group)
        {
            int hour = MainTabWindow_WorkTab.VisibleHour;
            return group.WorkGivers.Any(wg => pawn.GetPriority(wg, hour) > 0);
        }

        private static Passion GetPassionForGroup(Pawn pawn, WorkGroupSnapshot group)
        {
            Passion max = Passion.None;
            foreach (WorkTypeDef workType in group.UniqueWorkTypes)
            {
                if (workType == null)
                {
                    continue;
                }

                Passion p = pawn.skills.MaxPassionOfRelevantSkillsFor(workType);
                if ((int)p > (int)max)
                {
                    max = p;
                }
            }

            return max;
        }

        private static WorkActivityStatus ResolveStatus(WorkGroupStats stats, int mostRecentWorkTick, int nowTick)
        {
            if (stats.CapableCount == 0)
            {
                return WorkActivityStatus.Grey;
            }

            if (stats.EnabledCount == 0)
            {
                return WorkActivityStatus.Red;
            }

            if (mostRecentWorkTick < 0)
            {
                return WorkActivityStatus.Red;
            }

            int age = nowTick - mostRecentWorkTick;
            int green = WorkMonitorMod.Settings?.GreenStatusTicks ?? WorkMonitorSettings.TicksPerHour * 6;
            int yellow = WorkMonitorMod.Settings?.YellowStatusTicks ?? WorkMonitorSettings.TicksPerHour * 12;

            if (age <= green)
            {
                return WorkActivityStatus.Green;
            }

            if (age <= yellow)
            {
                return WorkActivityStatus.Yellow;
            }

            return WorkActivityStatus.Red;
        }
    }
}
