using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using WorkMonitor.Tracking;
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

            List<WorkGroupSnapshot> groups = new List<WorkGroupSnapshot>();
            groups.AddRange(new WorkTypeGroupProvider().GetGroups());
            groups.AddRange(WorkTabGroupsProvider.GetCustomGroups());
            groups.AddRange(new OtherWorkGroupProvider().GetGroups());

            cachedGroups = groups;
            lastCacheTick = tick;
            return cachedGroups;
        }
    }

    public static class WorkGroupStatsAggregator
    {
        public static WorkGroupStats Build(WorkGroupSnapshot group)
        {
            WorkGroupStats stats = new WorkGroupStats { Group = group };
            List<Pawn> colonists = WorkMonitorUtility.MonitorColonists().ToList();
            WorkActivityTracker tracker = WorkActivityTracker.EnsureRegistered();
            int nowTick = WorkMonitorUtility.CurrentTicksGame();
            int statsWindow = WorkMonitorMod.Settings?.StatsWindowTicks ?? WorkMonitorSettings.TicksPerHour * 24;
            int minHour = WorkMonitorUtility.CurrentHourIndex() - (WorkMonitorMod.Settings?.statsWindowHours ?? 24);
            WorkHistoryRingBuffer history = tracker?.GetGroupHistory(group.Key.StorageKey);

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

                int pawnJobs = 0;
                int pawnTicks = 0;
                int pawnLastTick = -1;

                foreach (WorkGiverDef wg in group.WorkGivers)
                {
                    WorkActivityRecord record = tracker?.GetRecord(pawn.thingIDNumber, wg.defName);
                    pawnJobs += tracker?.SumPawnWorkGiverJobs(pawn.thingIDNumber, wg.defName, minHour) ?? 0;
                    pawnTicks += tracker?.SumPawnWorkGiverTicks(pawn.thingIDNumber, wg.defName, minHour) ?? 0;
                    if (record != null)
                    {
                        pawnLastTick = Mathf.Max(pawnLastTick, record.lastWorkTick);
                    }
                }

                if (pawnJobs > 0 || pawnTicks > 0)
                {
                    stats.WorkedCount++;
                }

                if (enabled && pawnLastTick > mostRecentEnabledWorkTick)
                {
                    mostRecentEnabledWorkTick = pawnLastTick;
                }

                if (pawnTicks > 0 || pawnJobs > 0)
                {
                    stats.ColonistStats.Add(new ColonistWorkStat
                    {
                        Pawn = pawn,
                        Label = pawn.LabelShort,
                        JobCount = pawnJobs,
                        TicksSpent = pawnTicks,
                        Passion = GetPassionForGroup(pawn, group)
                    });
                }

                stats.TotalJobCount += pawnJobs;
                stats.TotalTicksSpent += pawnTicks;
            }

            if (history != null)
            {
                stats.TotalJobCount = history.SumJobCount(minHour);
                stats.TotalTicksSpent = history.SumTicksSpent(minHour);
            }

            foreach (WorkGiverDef wg in group.WorkGivers)
            {
                int wgJobs = 0;
                int wgTicks = 0;
                foreach (Pawn pawn in colonists)
                {
                    wgJobs += tracker?.SumPawnWorkGiverJobs(pawn.thingIDNumber, wg.defName, minHour) ?? 0;
                    wgTicks += tracker?.SumPawnWorkGiverTicks(pawn.thingIDNumber, wg.defName, minHour) ?? 0;
                }

                stats.WorkGiverStats.Add(new WorkGiverStat
                {
                    WorkGiver = wg,
                    Label = wg.label,
                    JobCount = wgJobs,
                    TicksSpent = wgTicks
                });
            }

            if (stats.TotalTicksSpent > 0)
            {
                foreach (ColonistWorkStat colonist in stats.ColonistStats)
                {
                    colonist.PercentOfGroupTime = colonist.TicksSpent / (float)stats.TotalTicksSpent * 100f;
                }
            }

            stats.ColonistStats = stats.ColonistStats.OrderByDescending(c => c.TicksSpent).ToList();
            stats.Status = ResolveStatus(stats, mostRecentEnabledWorkTick, nowTick);
            return stats;
        }

        public static List<WorkGroupStats> BuildAll()
        {
            return WorkGroupRegistry.GetAllGroups().Select(Build).ToList();
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
