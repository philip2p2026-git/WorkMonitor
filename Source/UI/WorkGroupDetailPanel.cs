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
        private const float JobsWidth = 42f;
        private const float WorkWidth = 52f;
        private const float TimeWidth = 50f;
        private const float KpiWidth = 52f;
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
            float viewHeight = 44f + stats.WorkGiverStats.Count * RowHeight + 36f + stats.ColonistStats.Count * RowHeight;
            Rect view = new Rect(0f, 0f, content.width - 16f, viewHeight);
            Widgets.BeginScrollView(content, ref scroll, view);

            float y = 0f;
            DrawWorkGiverTable(new Rect(0f, y, view.width, viewHeight - y), ref y);
            y += 12f;
            DrawColonistTable(new Rect(0f, y, view.width, viewHeight - y), ref y);

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

        private void DrawWorkGiverTable(Rect area, ref float y)
        {
            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(area.x, y, area.width, 22f), "WorkMonitor.WorkGivers".Translate());
            y += RowHeight;

            Rect headerRow = new Rect(area.x, y, area.width, RowHeight);
            DrawWorkGiverHeader(headerRow);
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

        private void DrawColonistTable(Rect area, ref float y)
        {
            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(area.x, y, area.width, 22f), "WorkMonitor.Colonists".Translate());
            y += RowHeight;

            Rect headerRow = new Rect(area.x, y, area.width, RowHeight);
            DrawColonistHeader(headerRow);
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

        private static void DrawWorkGiverHeader(Rect row)
        {
            Text.Font = GameFont.Tiny;
            Color prev = GUI.color;
            GUI.color = new Color(0.72f, 0.72f, 0.72f);

            GetWorkGiverColumns(row, out Rect labelCol, out Rect jobsCol, out Rect workCol, out Rect timeCol);
            Widgets.Label(labelCol, "WorkMonitor.WorkGiver".Translate());
            LabelRight(jobsCol, "WorkMonitor.Jobs".Translate());
            LabelRight(workCol, "WorkMonitor.Work".Translate());
            LabelRight(timeCol, "WorkMonitor.Time".Translate());

            GUI.color = prev;
        }

        private void DrawWorkGiverRow(Rect row, WorkGiverStat wg)
        {
            Text.Font = GameFont.Small;
            GetWorkGiverColumns(row, out Rect labelCol, out Rect jobsCol, out Rect workCol, out Rect timeCol);

            Widgets.Label(labelCol, wg.Label.Truncate(labelCol.width));
            LabelRight(jobsCol, wg.JobCount.ToString());
            LabelRight(workCol, WorkMonitorUtility.FormatWorkUnits(wg.WorkUnitsSpent));
            LabelRight(
                timeCol,
                WorkMonitorUtility.FormatDuration(wg.TicksSpent, WorkMonitorMod.Settings?.showTimeInHours ?? true));
        }

        private static void DrawColonistHeader(Rect row)
        {
            Text.Font = GameFont.Tiny;
            Color prev = GUI.color;
            GUI.color = new Color(0.72f, 0.72f, 0.72f);

            GetColonistColumns(row, out Rect nameCol, out Rect workCol, out Rect timeCol, out Rect kpiCol);
            Widgets.Label(nameCol, "WorkMonitor.Colonist".Translate());
            LabelRight(workCol, "WorkMonitor.Work".Translate());
            LabelRight(timeCol, "WorkMonitor.Time".Translate());
            LabelRight(kpiCol, "WorkMonitor.Kpi".Translate());

            GUI.color = prev;
        }

        private void DrawColonistRow(Rect row, ColonistWorkStat colonist)
        {
            Text.Font = GameFont.Small;
            GetColonistColumns(row, out Rect nameCol, out Rect workCol, out Rect timeCol, out Rect kpiCol);

            string passion = WorkMonitorUiUtility.PassionShort(colonist.Passion);
            Widgets.Label(nameCol, (passion + " " + colonist.Label).Trim().Truncate(nameCol.width));
            LabelRight(workCol, WorkMonitorUtility.FormatWorkUnits(colonist.WorkUnitsSpent));
            LabelRight(
                timeCol,
                WorkMonitorUtility.FormatDuration(colonist.TicksSpent, WorkMonitorMod.Settings?.showTimeInHours ?? true));
            LabelRight(kpiCol, WorkMonitorUtility.FormatWorkUnits(colonist.EfficiencyKpi) + "/h");
        }

        private static void GetWorkGiverColumns(Rect row, out Rect labelCol, out Rect jobsCol, out Rect workCol, out Rect timeCol)
        {
            float timeX = row.xMax - TimeWidth;
            float workX = timeX - ColumnGap - WorkWidth;
            float jobsX = workX - ColumnGap - JobsWidth;
            float labelX = row.x;
            float labelWidth = jobsX - ColumnGap - labelX;

            labelCol = new Rect(labelX, row.y, Mathf.Max(labelWidth, 80f), row.height);
            jobsCol = new Rect(jobsX, row.y, JobsWidth, row.height);
            workCol = new Rect(workX, row.y, WorkWidth, row.height);
            timeCol = new Rect(timeX, row.y, TimeWidth, row.height);
        }

        private static void GetColonistColumns(Rect row, out Rect nameCol, out Rect workCol, out Rect timeCol, out Rect kpiCol)
        {
            float kpiX = row.xMax - KpiWidth;
            float timeX = kpiX - ColumnGap - TimeWidth;
            float workX = timeX - ColumnGap - WorkWidth;
            float nameX = row.x;
            float nameWidth = workX - ColumnGap - nameX;

            nameCol = new Rect(nameX, row.y, Mathf.Max(nameWidth, 80f), row.height);
            workCol = new Rect(workX, row.y, WorkWidth, row.height);
            timeCol = new Rect(timeX, row.y, TimeWidth, row.height);
            kpiCol = new Rect(kpiX, row.y, KpiWidth, row.height);
        }

        private static void LabelRight(Rect rect, string text)
        {
            Text.Anchor = TextAnchor.MiddleRight;
            Widgets.Label(rect, text);
            Text.Anchor = TextAnchor.UpperLeft;
        }
    }
}
