using System;
using System.Linq;
using Verse;
using WorkMonitor.Groups;

namespace WorkMonitor.UI
{
    public static class BulkExpandUtility
    {
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
