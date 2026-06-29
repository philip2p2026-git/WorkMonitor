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

        private const float ColonistJobWidth = 40f;
        private const float ColonistWorkWidth = 48f;
        private const float ColonistTimeWidth = 50f;
        private const float ColonistShareWidth = 52f;

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
            out Rect workCol,
            out Rect timeCol,
            out Rect shareCol)
        {
            float shareX = row.xMax - ColonistShareWidth;
            float timeX = shareX - ColumnGap - ColonistTimeWidth;
            float workX = timeX - ColumnGap - ColonistWorkWidth;
            float jobX = workX - ColumnGap - ColonistJobWidth;

            jobCol = new Rect(jobX, row.y, ColonistJobWidth, row.height);
            workCol = new Rect(workX, row.y, ColonistWorkWidth, row.height);
            timeCol = new Rect(timeX, row.y, ColonistTimeWidth, row.height);
            shareCol = new Rect(shareX, row.y, ColonistShareWidth, row.height);
        }

        public static float ColonistGroupMetricsLeftEdge(Rect row)
        {
            GetColonistGroupColumns(row, out Rect jobCol, out _, out _, out _);
            return jobCol.x;
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
