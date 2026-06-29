using System.Collections.Generic;
using UnityEngine;
using Verse;
using WorkMonitor.Groups;

namespace WorkMonitor.UI
{
    public class WorkGroupDetailPanel
    {
        private const float RowHeight = 24f;
        private const float ChartHeight = 168f;
        private const float KpiJobWidth = 42f;
        private const float KpiWorkWidth = 52f;
        private const float JobsWidth = 42f;
        private const float WorkWidth = 52f;
        private const float TimeWidth = 50f;
        private const float ColumnGap = 10f;

        private readonly WorkGroupChartPanel chartPanel = new WorkGroupChartPanel();
        private Vector2 scroll;
        private WorkGroupStats stats;
        private List<WorkGroupStats> allStats = new List<WorkGroupStats>();

        public void SetGroup(WorkGroupSnapshot group)
        {
            allStats = WorkGroupStatsAggregator.BuildAll();
            stats = WorkGroupStatsAggregator.Build(group);
        }

        public void Draw(Rect rect, out bool back, out bool highlight)
        {
            back = false;
            highlight = false;

            if (stats == null)
            {
                return;
            }

            Text.Font = GameFont.Small;
            if (Widgets.ButtonText(new Rect(rect.x, rect.y, 70f, 26f), "WorkMonitor.Back".Translate()))
            {
                back = true;
                return;
            }

            Widgets.Label(
                new Rect(rect.x + 76f, rect.y + 2f, rect.width - 76f - 128f, 22f),
                "WorkMonitor.DetailTitle".Translate(stats.Group.Label));

            Rect highlightRect = new Rect(rect.xMax - 120f, rect.y, 120f, 26f);
            if (Widgets.ButtonText(highlightRect, "WorkMonitor.HighlightShort".Translate()))
            {
                highlight = true;
            }
            TooltipHandler.TipRegion(highlightRect, "WorkMonitor.HighlightInWorkTab".Translate());

            Rect header = new Rect(rect.x, rect.y + 32f, rect.width, 36f);
            DrawHeader(header);

            Rect chartRect = new Rect(rect.x, header.yMax + 4f, rect.width, ChartHeight);
            chartPanel.Draw(chartRect, stats, allStats);

            Rect content = new Rect(rect.x, chartRect.yMax + 6f, rect.width, rect.yMax - chartRect.yMax - 12f);
            int rowCount = stats.ColonistStats.Count + stats.WorkGiverStats.Count;
            float viewHeight = 108f + rowCount * RowHeight;
            Rect view = new Rect(0f, 0f, content.width - 16f, viewHeight);
            Widgets.BeginScrollView(content, ref scroll, view);

            float y = 0f;
            DrawColonistTable(new Rect(0f, y, view.width, viewHeight - y), ref y);
            y += 12f;
            DrawWorkGiverTable(new Rect(0f, y, view.width, viewHeight - y), ref y);

            Widgets.EndScrollView();
        }

        private void DrawHeader(Rect rect)
        {
            Color dot = WorkMonitorUiUtility.StatusColor(stats.Status);
            Widgets.DrawBoxSolid(new Rect(rect.x, rect.y + 5f, 10f, 10f), dot);

            Text.Font = GameFont.Tiny;
            string summary = string.Format(
                "capable {0} · enabled {1} · worked {2} · {3}",
                stats.CapableCount,
                stats.EnabledCount,
                stats.WorkedCount,
                WorkMonitorUiUtility.FormatInterestRatio(stats));
            Widgets.Label(new Rect(rect.x + 14f, rect.y, rect.width - 14f, 16f), summary);

            string totals = "WorkMonitor.JobsWorkUnitsSummary".Translate(
                stats.TotalJobCount,
                WorkMonitorUtility.FormatWorkUnits(stats.TotalWorkUnits),
                WorkMonitorUtility.FormatDuration(stats.TotalTicksSpent, WorkMonitorMod.Settings?.showTimeInHours ?? true));
            Widgets.Label(new Rect(rect.x + 14f, rect.y + 18f, rect.width - 14f, 16f), totals);
        }

        private void DrawColonistTable(Rect area, ref float y)
        {
            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(area.x, y, area.width, 22f), "WorkMonitor.Colonists".Translate());
            y += RowHeight;

            DrawColonistHeader(new Rect(area.x, y, area.width, RowHeight));
            y += RowHeight;

            int rowIndex = 0;
            foreach (ColonistWorkStat colonist in stats.ColonistStats)
            {
                Rect row = new Rect(area.x, y, area.width, RowHeight);
                if (rowIndex % 2 == 1)
                {
                    Widgets.DrawBoxSolid(row, new Color(1f, 1f, 1f, 0.03f));
                }

                DrawColonistRow(row, colonist);
                y += RowHeight;
                rowIndex++;
            }
        }

        private void DrawWorkGiverTable(Rect area, ref float y)
        {
            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(area.x, y, area.width, 22f), "WorkMonitor.WorkGivers".Translate());
            y += RowHeight;

            DrawWorkGiverHeader(new Rect(area.x, y, area.width, RowHeight));
            y += RowHeight;

            int rowIndex = 0;
            foreach (WorkGiverStat wg in stats.WorkGiverStats)
            {
                Rect row = new Rect(area.x, y, area.width, RowHeight);
                if (rowIndex % 2 == 1)
                {
                    Widgets.DrawBoxSolid(row, new Color(1f, 1f, 1f, 0.03f));
                }

                DrawWorkGiverRow(row, wg);
                y += RowHeight;
                rowIndex++;
            }
        }

        private static void DrawColonistHeader(Rect row)
        {
            Text.Font = GameFont.Tiny;
            Color prev = GUI.color;
            GUI.color = new Color(0.72f, 0.72f, 0.72f);

            GetDetailTableColumns(
                row,
                out Rect labelCol,
                out Rect kpiJobCol,
                out Rect kpiWorkCol,
                out Rect jobsCol,
                out Rect workCol,
                out Rect timeCol);

            Widgets.Label(labelCol, "WorkMonitor.Colonist".Translate());
            LabelRight(kpiJobCol, "WorkMonitor.KpiJobs".Translate());
            LabelRight(kpiWorkCol, "WorkMonitor.KpiWork".Translate());
            LabelRight(jobsCol, "WorkMonitor.Jobs".Translate());
            LabelRight(workCol, "WorkMonitor.Work".Translate());
            LabelRight(timeCol, "WorkMonitor.Time".Translate());

            GUI.color = prev;
        }

        private void DrawColonistRow(Rect row, ColonistWorkStat colonist)
        {
            Text.Font = GameFont.Small;
            GetDetailTableColumns(
                row,
                out Rect labelCol,
                out Rect kpiJobCol,
                out Rect kpiWorkCol,
                out Rect jobsCol,
                out Rect workCol,
                out Rect timeCol);

            string passion = WorkMonitorUiUtility.PassionShort(colonist.Passion);
            Widgets.Label(labelCol, (passion + " " + colonist.Label).Trim().Truncate(labelCol.width));
            LabelRight(kpiJobCol, FormatPerHour(colonist.JobsPerHour, integer: true));
            LabelRight(kpiWorkCol, FormatPerHour(colonist.WorkUnitsPerHour, integer: false));
            LabelRight(jobsCol, colonist.JobCount.ToString());
            LabelRight(workCol, WorkMonitorUtility.FormatWorkUnits(colonist.WorkUnitsSpent));
            LabelRight(
                timeCol,
                WorkMonitorUtility.FormatDuration(colonist.TicksSpent, WorkMonitorMod.Settings?.showTimeInHours ?? true));
        }

        private static void DrawWorkGiverHeader(Rect row)
        {
            Text.Font = GameFont.Tiny;
            Color prev = GUI.color;
            GUI.color = new Color(0.72f, 0.72f, 0.72f);

            GetDetailTableColumns(
                row,
                out Rect labelCol,
                out Rect kpiJobCol,
                out Rect kpiWorkCol,
                out Rect jobsCol,
                out Rect workCol,
                out Rect timeCol);

            Rect nameHeader = Rect.MinMaxRect(labelCol.xMin, labelCol.yMin, kpiWorkCol.xMax, labelCol.yMax);
            Widgets.Label(nameHeader, "WorkMonitor.WorkGiver".Translate());
            LabelRight(jobsCol, "WorkMonitor.Jobs".Translate());
            LabelRight(workCol, "WorkMonitor.Work".Translate());
            LabelRight(timeCol, "WorkMonitor.Time".Translate());

            GUI.color = prev;
        }

        private void DrawWorkGiverRow(Rect row, WorkGiverStat wg)
        {
            Text.Font = GameFont.Small;
            GetDetailTableColumns(
                row,
                out Rect labelCol,
                out Rect kpiJobCol,
                out Rect kpiWorkCol,
                out Rect jobsCol,
                out Rect workCol,
                out Rect timeCol);

            Rect nameCol = Rect.MinMaxRect(labelCol.xMin, labelCol.yMin, kpiWorkCol.xMax, labelCol.yMax);
            Widgets.Label(nameCol, wg.Label.Truncate(nameCol.width));
            LabelRight(jobsCol, wg.JobCount.ToString());
            LabelRight(workCol, WorkMonitorUtility.FormatWorkUnits(wg.WorkUnitsSpent));
            LabelRight(
                timeCol,
                WorkMonitorUtility.FormatDuration(wg.TicksSpent, WorkMonitorMod.Settings?.showTimeInHours ?? true));
        }

        private static void GetDetailTableColumns(
            Rect row,
            out Rect labelCol,
            out Rect kpiJobCol,
            out Rect kpiWorkCol,
            out Rect jobsCol,
            out Rect workCol,
            out Rect timeCol)
        {
            float timeX = row.xMax - TimeWidth;
            float workX = timeX - ColumnGap - WorkWidth;
            float jobsX = workX - ColumnGap - JobsWidth;
            float kpiWorkX = jobsX - ColumnGap - KpiWorkWidth;
            float kpiJobX = kpiWorkX - ColumnGap - KpiJobWidth;
            float labelX = row.x;
            float labelWidth = kpiJobX - ColumnGap - labelX;

            labelCol = new Rect(labelX, row.y, Mathf.Max(labelWidth, 60f), row.height);
            kpiJobCol = new Rect(kpiJobX, row.y, KpiJobWidth, row.height);
            kpiWorkCol = new Rect(kpiWorkX, row.y, KpiWorkWidth, row.height);
            jobsCol = new Rect(jobsX, row.y, JobsWidth, row.height);
            workCol = new Rect(workX, row.y, WorkWidth, row.height);
            timeCol = new Rect(timeX, row.y, TimeWidth, row.height);
        }

        private static string FormatPerHour(float value, bool integer)
        {
            if (value <= 0f)
            {
                return "—";
            }

            string formatted = integer
                ? value.ToString("0.#")
                : WorkMonitorUtility.FormatWorkUnits(value);
            return formatted + "/h";
        }

        private static void LabelRight(Rect rect, string text)
        {
            Text.Anchor = TextAnchor.MiddleRight;
            Widgets.Label(rect, text);
            Text.Anchor = TextAnchor.UpperLeft;
        }
    }
}
