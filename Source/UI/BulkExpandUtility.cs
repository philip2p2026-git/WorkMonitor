using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using WorkMonitor.Groups;

namespace WorkMonitor.UI
{
    public static class BulkExpandUtility
    {
        public const int UnassignedBacklogPawnId = 0;

        public static void ExpandOneLevel(
            bool allLevel1Expanded,
            Action expandLevel1,
            Action expandLevel2)
        {
            if (!allLevel1Expanded)
            {
                expandLevel1();
            }
            else
            {
                expandLevel2();
            }
        }

        public static void CollapseOneLevel(
            bool anyLevel2Expanded,
            Action collapseLevel2,
            Action collapseLevel1)
        {
            if (anyLevel2Expanded)
            {
                collapseLevel2();
            }
            else
            {
                collapseLevel1();
            }
        }

        public static bool IsVisibleWorkGiverRow(WorkGiverDetailStats detail, WorkGiverStat mapStat)
        {
            bool colonist = detail != null && detail.ColonistStats.Count > 0;
            bool map = mapStat != null && (mapStat.MapOpenTasks > 0 || mapStat.MapWorkLeft > 0f);
            return colonist || map;
        }

        public static bool IsMapOnlyWorkGiver(WorkGiverDetailStats detail, WorkGiverStat mapStat)
        {
            bool colonist = detail != null && detail.ColonistStats.Count > 0;
            bool map = mapStat != null && (mapStat.MapOpenTasks > 0 || mapStat.MapWorkLeft > 0f);
            return map && !colonist;
        }

        public static bool HasMapOnlyBacklog(
            WorkGroupStats stats,
            Func<WorkGiverDef, WorkGiverDetailStats> getDetail)
        {
            return GetMapOnlyWorkGivers(stats, getDetail).Count > 0;
        }

        public static List<WorkGiverDef> GetMapOnlyWorkGivers(
            WorkGroupStats stats,
            Func<WorkGiverDef, WorkGiverDetailStats> getDetail)
        {
            if (stats?.Group == null)
            {
                return new List<WorkGiverDef>();
            }

            var ranked = new List<(WorkGiverDef workGiver, float mapWork, int mapOpen)>();
            foreach (WorkGiverDef workGiver in stats.Group.WorkGivers)
            {
                WorkGiverDetailStats detail = getDetail?.Invoke(workGiver);
                WorkGiverStat mapStat = stats.WorkGiverStats.Find(wg => wg.WorkGiver == workGiver);
                if (!IsMapOnlyWorkGiver(detail, mapStat))
                {
                    continue;
                }

                ranked.Add((workGiver, RankMapWork(mapStat), RankMapOpenTasks(mapStat)));
            }

            return ranked
                .OrderByDescending(entry => entry.mapWork)
                .ThenByDescending(entry => entry.mapOpen)
                .Select(entry => entry.workGiver)
                .ToList();
        }

        public static void SumMapOnlyMetrics(
            WorkGroupStats stats,
            Func<WorkGiverDef, WorkGiverDetailStats> getDetail,
            out int mapOpenTasks,
            out int mapNewTodayOpenTasks,
            out float mapWorkLeft,
            out float mapNewTodayWorkLeft)
        {
            mapOpenTasks = 0;
            mapNewTodayOpenTasks = 0;
            mapWorkLeft = 0f;
            mapNewTodayWorkLeft = 0f;

            foreach (WorkGiverDef workGiver in GetMapOnlyWorkGivers(stats, getDetail))
            {
                WorkGiverStat mapStat = stats.WorkGiverStats.Find(wg => wg.WorkGiver == workGiver);
                if (mapStat == null)
                {
                    continue;
                }

                mapOpenTasks += mapStat.MapOpenTasks;
                mapNewTodayOpenTasks += mapStat.MapNewTodayOpenTasks;
                mapWorkLeft += mapStat.MapWorkLeft;
                mapNewTodayWorkLeft += mapStat.MapNewTodayWorkLeft;
            }
        }

        public static int RankTicks(WorkGiverDetailStats detail)
        {
            return detail?.ColonistStats.Sum(c => c.TicksSpent) ?? 0;
        }

        public static float RankMapWork(WorkGiverStat mapStat)
        {
            return mapStat?.MapWorkLeft ?? 0f;
        }

        public static int RankMapOpenTasks(WorkGiverStat mapStat)
        {
            return mapStat?.MapOpenTasks ?? 0;
        }
    }
}
