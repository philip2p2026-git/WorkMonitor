using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using WorkMonitor.Tracking;

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
                float pawnWork = 0f;

                foreach (WorkGiverDef wg in group.WorkGivers)
                {
                    pawnJobs += tracker?.SumPawnWorkGiverJobs(pawn.thingIDNumber, wg.defName, minHour) ?? 0;
                    pawnTicks += tracker?.SumPawnWorkGiverTicks(pawn.thingIDNumber, wg.defName, minHour) ?? 0;
                    pawnWork += tracker?.SumPawnWorkGiverWorkUnits(pawn.thingIDNumber, wg.defName, minHour) ?? 0f;
                }

                if (pawnJobs <= 0 && pawnTicks <= 0 && pawnWork <= 0f)
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
                    WorkUnitsSpent = pawnWork,
                    GroupJobCount = groupStats.TotalJobCount,
                    GroupWorkUnits = groupStats.TotalWorkUnits,
                    GroupTicksSpent = groupStats.TotalTicksSpent
                });

                stats.TotalJobCount += pawnJobs;
                stats.TotalTicksSpent += pawnTicks;
                stats.TotalWorkUnits += pawnWork;
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
    }
}
