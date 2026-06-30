using UnityEngine;
using Verse;

namespace WorkMonitor.UI
{
    public static class WorkMonitorTableColumns
    {
        private const float ExistJobWidth = 66f;
        private const float ExistWorkWidth = 78f;
        private const float JobProcessedWidth = 108f;
        private const float WorkProcessedWidth = 114f;
        private const float ColumnGap = 6f;

        private const float ColonistJobWidth = 54f;
        private const float ColonistEndlessWidth = 60f;
        private const float ColonistWorkWidth = 66f;
        private const float ColonistWalkWidth = 60f;
        private const float ColonistActiveWorkWidth = 60f;
        private const float ColonistShareWidth = 72f;

        private const float GroupDetailWalkWidth = 60f;
        private const float GroupDetailActiveWorkWidth = 60f;

        public const float ExpandButtonWidth = 20f;
        public const float ColonistIconSize = 18f;
        public const float ColonistIconGap = 4f;
        private const float GroupDetailKpiJobWidth = 63f;
        private const float GroupDetailKpiWorkWidth = 78f;
        private const float GroupDetailJobsWidth = 63f;
        private const float GroupDetailEndlessWidth = 66f;
        private const float GroupDetailWorkWidth = 78f;
        private const float GroupDetailColumnGap = 10f;

        public static void GetGroupDetailColonistTableColumns(
            Rect row,
            out Rect portraitCol,
            out Rect iconsCol,
            out Rect labelCol,
            out Rect kpiJobCol,
            out Rect kpiWorkCol,
            out Rect jobsCol,
            out Rect endlessCol,
            out Rect workCol,
            out Rect walkCol,
            out Rect activeWorkCol)
        {
            float activeWorkX = row.xMax - GroupDetailActiveWorkWidth;
            float walkX = activeWorkX - GroupDetailColumnGap - GroupDetailWalkWidth;
            float workX = walkX - GroupDetailColumnGap - GroupDetailWorkWidth;
            float endlessX = workX - GroupDetailColumnGap - GroupDetailEndlessWidth;
            float jobsX = endlessX - GroupDetailColumnGap - GroupDetailJobsWidth;
            float kpiWorkX = jobsX - GroupDetailColumnGap - GroupDetailKpiWorkWidth;
            float kpiJobX = kpiWorkX - GroupDetailColumnGap - GroupDetailKpiJobWidth;
            float iconX = kpiJobX - GroupDetailColumnGap - ColonistIconSize;
            float labelX = row.x + ExpandButtonWidth + ColonistIconGap + WorkMonitorUiUtility.ColonistPortraitSize + ColonistIconGap;
            float labelWidth = iconX - GroupDetailColumnGap - labelX;
            float portraitX = row.x + ExpandButtonWidth + ColonistIconGap;

            portraitCol = new Rect(portraitX, row.y, WorkMonitorUiUtility.ColonistPortraitSize, row.height);
            labelCol = new Rect(labelX, row.y, Mathf.Max(labelWidth, 48f), row.height);
            iconsCol = new Rect(iconX, row.y, ColonistIconSize, row.height);
            kpiJobCol = new Rect(kpiJobX, row.y, GroupDetailKpiJobWidth, row.height);
            kpiWorkCol = new Rect(kpiWorkX, row.y, GroupDetailKpiWorkWidth, row.height);
            jobsCol = new Rect(jobsX, row.y, GroupDetailJobsWidth, row.height);
            endlessCol = new Rect(endlessX, row.y, GroupDetailEndlessWidth, row.height);
            workCol = new Rect(workX, row.y, GroupDetailWorkWidth, row.height);
            walkCol = new Rect(walkX, row.y, GroupDetailWalkWidth, row.height);
            activeWorkCol = new Rect(activeWorkX, row.y, GroupDetailActiveWorkWidth, row.height);
        }

        public static void GetWorkGiverDetailColonistTableColumns(
            Rect row,
            out Rect portraitCol,
            out Rect iconsCol,
            out Rect labelCol,
            out Rect kpiJobCol,
            out Rect kpiWorkCol,
            out Rect jobsCol,
            out Rect endlessCol,
            out Rect workCol,
            out Rect walkCol,
            out Rect activeWorkCol)
        {
            float activeWorkX = row.xMax - GroupDetailActiveWorkWidth;
            float walkX = activeWorkX - GroupDetailColumnGap - GroupDetailWalkWidth;
            float workX = walkX - GroupDetailColumnGap - GroupDetailWorkWidth;
            float endlessX = workX - GroupDetailColumnGap - GroupDetailEndlessWidth;
            float jobsX = endlessX - GroupDetailColumnGap - GroupDetailJobsWidth;
            float kpiWorkX = jobsX - GroupDetailColumnGap - GroupDetailKpiWorkWidth;
            float kpiJobX = kpiWorkX - GroupDetailColumnGap - GroupDetailKpiJobWidth;
            float iconX = kpiJobX - GroupDetailColumnGap - ColonistIconSize;
            float labelX = row.x + WorkMonitorUiUtility.ColonistPortraitSize + ColonistIconGap;
            float labelWidth = iconX - GroupDetailColumnGap - labelX;

            portraitCol = new Rect(row.x, row.y, WorkMonitorUiUtility.ColonistPortraitSize, row.height);
            labelCol = new Rect(labelX, row.y, Mathf.Max(labelWidth, 48f), row.height);
            iconsCol = new Rect(iconX, row.y, ColonistIconSize, row.height);
            kpiJobCol = new Rect(kpiJobX, row.y, GroupDetailKpiJobWidth, row.height);
            kpiWorkCol = new Rect(kpiWorkX, row.y, GroupDetailKpiWorkWidth, row.height);
            jobsCol = new Rect(jobsX, row.y, GroupDetailJobsWidth, row.height);
            endlessCol = new Rect(endlessX, row.y, GroupDetailEndlessWidth, row.height);
            workCol = new Rect(workX, row.y, GroupDetailWorkWidth, row.height);
            walkCol = new Rect(walkX, row.y, GroupDetailWalkWidth, row.height);
            activeWorkCol = new Rect(activeWorkX, row.y, GroupDetailActiveWorkWidth, row.height);
        }

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
