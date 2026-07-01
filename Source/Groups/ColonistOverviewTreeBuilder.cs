using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using WorkMonitor.Tracking;
using WorkMonitor.UI;

namespace WorkMonitor.Groups
{
    public class ColonistOverviewMapOnlyEntry
    {
        public WorkGiverDef WorkGiver;
        public WorkGiverStat MapStat;
        public string Label;
    }

    public class ColonistOverviewGroupNode
    {
        public WorkGroupSnapshot Group;
        public WorkGroupStats GroupStats;
        public List<ColonistWorkGiverStat> ColonistWorkGivers = new List<ColonistWorkGiverStat>();
        public List<ColonistOverviewMapOnlyEntry> MapOnlyWorkGivers = new List<ColonistOverviewMapOnlyEntry>();
    }

    public class ColonistOverviewNode
    {
        public int PawnId;
        public ColonistWorkStat Summary;
        public List<ColonistOverviewGroupNode> Groups = new List<ColonistOverviewGroupNode>();

        public bool IsUnassigned => PawnId == BulkExpandUtility.UnassignedBacklogPawnId;
    }

    public static class ColonistOverviewTreeBuilder
    {
        public static List<ColonistOverviewNode> Build(int rangeHours, List<WorkGroupStats> allStats)
        {
            allStats ??= WorkGroupStatsAggregator.BuildAll(rangeHours);
            int minHour = WorkMonitorUtility.CurrentHourIndex() - rangeHours;
            WorkActivityTracker tracker = WorkActivityTracker.EnsureRegistered();

            var pawnTotals = new Dictionary<int, (int jobs, int endless, int ticks, float workUnits)>();
            foreach (int pawnId in ColonistWorkQuery.GetColonistIdsWithAnyWork(minHour))
            {
                if (pawnId <= 0)
                {
                    continue;
                }

                int jobs = 0;
                int endless = 0;
                int ticks = 0;
                float workUnits = 0f;

                foreach (WorkGroupStats groupStats in allStats)
                {
                    foreach (WorkGiverDef wg in groupStats.Group.WorkGivers)
                    {
                        jobs += tracker?.SumPawnWorkGiverJobs(pawnId, wg.defName, minHour) ?? 0;
                        endless += tracker?.SumPawnWorkGiverEndlessJobs(pawnId, wg.defName, minHour) ?? 0;
                        ticks += tracker?.SumPawnWorkGiverTicks(pawnId, wg.defName, minHour) ?? 0;
                        workUnits += tracker?.SumPawnWorkGiverWorkUnits(pawnId, wg.defName, minHour) ?? 0f;
                    }
                }

                if (jobs <= 0 && endless <= 0 && ticks <= 0 && workUnits <= 0f)
                {
                    continue;
                }

                pawnTotals[pawnId] = (jobs, endless, ticks, workUnits);
            }

            var nodes = new List<ColonistOverviewNode>();
            foreach (var kvp in pawnTotals.OrderByDescending(e => e.Value.ticks))
            {
                int pawnId = kvp.Key;
                var node = new ColonistOverviewNode
                {
                    PawnId = pawnId,
                    Summary = BuildColonistSummary(pawnId, kvp.Value, tracker)
                };

                foreach (WorkGroupStats groupStats in allStats)
                {
                    ColonistGroupWorkDetail detail = ColonistStatsAggregator.BuildGroupDetail(pawnId, groupStats.Group, rangeHours);
                    if (detail == null || detail.WorkGiverStats.Count == 0)
                    {
                        continue;
                    }

                    node.Groups.Add(new ColonistOverviewGroupNode
                    {
                        Group = groupStats.Group,
                        GroupStats = groupStats,
                        ColonistWorkGivers = detail.WorkGiverStats
                    });
                }

                if (node.Groups.Count > 0)
                {
                    nodes.Add(node);
                }
            }

            ColonistOverviewNode unassigned = BuildUnassignedNode(allStats, rangeHours);
            if (unassigned != null)
            {
                nodes.Add(unassigned);
            }

            return nodes;
        }

        private static ColonistOverviewNode BuildUnassignedNode(List<WorkGroupStats> allStats, int rangeHours)
        {
            var node = new ColonistOverviewNode
            {
                PawnId = BulkExpandUtility.UnassignedBacklogPawnId,
                Summary = new ColonistWorkStat
                {
                    PawnId = BulkExpandUtility.UnassignedBacklogPawnId,
                    Label = "WorkMonitor.UnassignedBacklog".Translate()
                }
            };

            foreach (WorkGroupStats groupStats in allStats)
            {
                List<WorkGiverDef> mapOnly = BulkExpandUtility.GetMapOnlyWorkGivers(
                    groupStats,
                    wg => WorkGiverStatsAggregator.Build(groupStats.Group, wg, rangeHours));

                if (mapOnly.Count == 0)
                {
                    continue;
                }

                var groupNode = new ColonistOverviewGroupNode
                {
                    Group = groupStats.Group,
                    GroupStats = groupStats
                };

                foreach (WorkGiverDef workGiver in mapOnly)
                {
                    WorkGiverStat mapStat = groupStats.WorkGiverStats.Find(wg => wg.WorkGiver == workGiver);
                    groupNode.MapOnlyWorkGivers.Add(new ColonistOverviewMapOnlyEntry
                    {
                        WorkGiver = workGiver,
                        MapStat = mapStat,
                        Label = WorkGiverLabelUtility.Format(workGiver)
                    });
                }

                node.Groups.Add(groupNode);
            }

            return node.Groups.Count > 0 ? node : null;
        }

        private static ColonistWorkStat BuildColonistSummary(
            int pawnId,
            (int jobs, int endless, int ticks, float workUnits) totals,
            WorkActivityTracker tracker)
        {
            Pawn pawn = ColonistWorkQuery.TryResolvePawn(pawnId);
            return new ColonistWorkStat
            {
                PawnId = pawnId,
                Pawn = pawn,
                Label = ColonistWorkQuery.ResolveLabel(pawnId, tracker),
                IsAbsent = ColonistWorkQuery.IsAbsent(pawnId, tracker),
                JobCount = totals.jobs,
                EndlessJobCount = totals.endless,
                TicksSpent = totals.ticks,
                WorkUnitsSpent = totals.workUnits
            };
        }
    }
}
