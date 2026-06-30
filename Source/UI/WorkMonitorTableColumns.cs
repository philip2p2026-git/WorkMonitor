using UnityEngine;
using Verse;

namespace WorkMonitor.UI
{
    public static class WorkMonitorTableColumns
    {
        private const float ExistJobWidth = 44f;
        private const float ExistWorkWidth = 52f;
        private const float JobProcessedWidth = 72f;
        private const float WorkProcessedWidth = 76f;
        private const float ColumnGap = 6f;

        private const float ColonistJobWidth = 36f;
        private const float ColonistEndlessWidth = 40f;
        private const float ColonistWorkWidth = 44f;
        private const float ColonistWalkWidth = 40f;
        private const float ColonistActiveWorkWidth = 40f;
        private const float ColonistShareWidth = 48f;

        private const float GroupDetailWalkWidth = 40f;
        private const float GroupDetailActiveWorkWidth = 40f;

        public static void GetOverviewMetricColumns(
            Rect row,
            out Rect existJobCol,
            out Rect existWorkCol,
            out Rect jobProcessedCol,
            out Rect workProcessedCol)
        {
            float workProcessedX = row.xMax - WorkProcessedWidth;
            float jobProcessedX = workProcessedX - ColumnGap - JobProcessedWidth;
            float existWorkX = jobProcessedX - ColumnGap - ExistWorkWidth;
            float existJobX = existWorkX - ColumnGap - ExistJobWidth;

            existJobCol = new Rect(existJobX, row.y, ExistJobWidth, row.height);
            existWorkCol = new Rect(existWorkX, row.y, ExistWorkWidth, row.height);
            jobProcessedCol = new Rect(jobProcessedX, row.y, JobProcessedWidth, row.height);
            workProcessedCol = new Rect(workProcessedX, row.y, WorkProcessedWidth, row.height);
        }

        public static float OverviewMetricsLeftEdge(Rect row)
        {
            GetOverviewMetricColumns(row, out Rect existJobCol, out _, out _, out _);
            return existJobCol.x;
        }

        public static void GetColonistGroupColumns(
            Rect row,
            out Rect jobCol,
            out Rect endlessCol,
            out Rect workCol,
            out Rect walkCol,
            out Rect activeWorkCol,
            out Rect shareCol)
        {
            float shareX = row.xMax - ColonistShareWidth;
            float activeWorkX = shareX - ColumnGap - ColonistActiveWorkWidth;
            float walkX = activeWorkX - ColumnGap - ColonistWalkWidth;
            float workX = walkX - ColumnGap - ColonistWorkWidth;
            float endlessX = workX - ColumnGap - ColonistEndlessWidth;
            float jobX = endlessX - ColumnGap - ColonistJobWidth;

            jobCol = new Rect(jobX, row.y, ColonistJobWidth, row.height);
            endlessCol = new Rect(endlessX, row.y, ColonistEndlessWidth, row.height);
            workCol = new Rect(workX, row.y, ColonistWorkWidth, row.height);
            walkCol = new Rect(walkX, row.y, ColonistWalkWidth, row.height);
            activeWorkCol = new Rect(activeWorkX, row.y, ColonistActiveWorkWidth, row.height);
            shareCol = new Rect(shareX, row.y, ColonistShareWidth, row.height);
        }

        public static float ColonistGroupMetricsLeftEdge(Rect row)
        {
            GetColonistGroupColumns(row, out Rect jobCol, out _, out _, out _, out _, out _);
            return jobCol.x;
        }

        public static void GetColonistWorkGiverColumns(
            Rect row,
            out Rect jobCol,
            out Rect endlessCol,
            out Rect workCol,
            out Rect walkCol,
            out Rect activeWorkCol,
            out Rect shareCol)
        {
            float shareX = row.xMax - ColonistShareWidth;
            float activeWorkX = shareX - ColumnGap - ColonistActiveWorkWidth;
            float walkX = activeWorkX - ColumnGap - ColonistWalkWidth;
            float workX = walkX - ColumnGap - ColonistWorkWidth;
            float endlessX = workX - ColumnGap - ColonistEndlessWidth;
            float jobX = endlessX - ColumnGap - ColonistJobWidth;

            jobCol = new Rect(jobX, row.y, ColonistJobWidth, row.height);
            endlessCol = new Rect(endlessX, row.y, ColonistEndlessWidth, row.height);
            workCol = new Rect(workX, row.y, ColonistWorkWidth, row.height);
            walkCol = new Rect(walkX, row.y, ColonistWalkWidth, row.height);
            activeWorkCol = new Rect(activeWorkX, row.y, ColonistActiveWorkWidth, row.height);
            shareCol = new Rect(shareX, row.y, ColonistShareWidth, row.height);
        }

        public static float ColonistWorkGiverMetricsLeftEdge(Rect row)
        {
            GetColonistWorkGiverColumns(row, out Rect jobCol, out _, out _, out _, out _, out _);
            return jobCol.x;
        }

        public static void GetGroupDetailColonistTimeColumns(
            Rect row,
            float jobsColX,
            out Rect walkCol,
            out Rect activeWorkCol)
        {
            float activeWorkX = row.xMax - GroupDetailActiveWorkWidth;
            float walkX = activeWorkX - ColumnGap - GroupDetailWalkWidth;
            walkCol = new Rect(walkX, row.y, GroupDetailWalkWidth, row.height);
            activeWorkCol = new Rect(activeWorkX, row.y, GroupDetailActiveWorkWidth, row.height);
        }

        public static void DrawOverviewMetricHeader(
            Rect row,
            System.Action<Rect, string> labelRight)
        {
            GetOverviewMetricColumns(row, out Rect existJobCol, out Rect existWorkCol, out Rect jobProcessedCol, out Rect workProcessedCol);
            labelRight(existJobCol, "WorkMonitor.ExistJob".Translate());
            labelRight(existWorkCol, "WorkMonitor.ExistWork".Translate());
            labelRight(jobProcessedCol, "WorkMonitor.JobProcessed".Translate());
            labelRight(workProcessedCol, "WorkMonitor.WorkProcessed".Translate());
            TooltipHandler.TipRegion(existJobCol, "WorkMonitor.ExistJobTip".Translate());
            TooltipHandler.TipRegion(existWorkCol, "WorkMonitor.ExistWorkTip".Translate());
            TooltipHandler.TipRegion(jobProcessedCol, "WorkMonitor.JobProcessedTip".Translate());
            TooltipHandler.TipRegion(workProcessedCol, "WorkMonitor.WorkProcessedTip".Translate());
        }
    }
}
