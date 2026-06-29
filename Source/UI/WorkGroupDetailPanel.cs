using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using WorkMonitor.Groups;

namespace WorkMonitor.UI
{
    public class WorkGroupDetailPanel
    {
        private const float RowHeight = 24f;
        private const float ChartHeight = 168f;
        private const float ColonistIconSize = 18f;
        private const float ColonistIconsWidth = ColonistIconSize;
        private const float KpiJobWidth = 42f;
        private const float KpiWorkWidth = 52f;
        private const float JobsWidth = 42f;
        private const float WorkWidth = 52f;
        private const float WalkWidth = 40f;
        private const float ActiveWorkWidth = 40f;
        private const float ColumnGap = 10f;
        private const float GroupDropdownWidth = 160f;

        private readonly WorkGroupChartPanel chartPanel = new WorkGroupChartPanel();
        private Vector2 scroll;
        private WorkGroupStats stats;
        private List<WorkGroupStats> allStats = new List<WorkGroupStats>();
        private WorkGroupSnapshot pendingGroupSelection;

        public WorkGroupSnapshot CurrentGroup => stats?.Group;

        public void SetGroup(WorkGroupSnapshot group)
        {
            allStats = WorkGroupStatsAggregator.BuildAll();
            stats = WorkGroupStatsAggregator.Build(group);
        }

        public void Draw(Rect rect, out bool back, out bool colonistClicked, out ColonistWorkStat selectedColonist, out WorkGroupSnapshot groupChanged)
        {
            back = false;
            colonistClicked = false;
            selectedColonist = null;
            groupChanged = null;

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

            Rect dropdownRect = new Rect(rect.x + 76f, rect.y, GroupDropdownWidth, 26f);
            List<FloatMenuOption> groupOptions = WorkMonitorDropdownUtility.BuildOptions(
                WorkGroupRegistry.GetAllGroups(),
                g => g.Label,
                g =>
                {
                    if (stats == null || g.Key.StorageKey != stats.Group.Key.StorageKey)
                    {
                        pendingGroupSelection = g;
                    }
                });
            WorkMonitorDropdownUtility.DrawDropdown(dropdownRect, stats.Group.Label, groupOptions);

            Rect header = new Rect(rect.x, rect.y + 32f, rect.width, 52f);
            DrawHeader(header);

            Rect chartRect = new Rect(rect.x, header.yMax + 4f, rect.width, ChartHeight);
            chartPanel.Draw(chartRect, stats, allStats);

            Rect content = new Rect(rect.x, chartRect.yMax + 6f, rect.width, rect.yMax - chartRect.yMax - 12f);
            int rowCount = stats.ColonistStats.Count + stats.WorkGiverStats.Count + 1;
            float viewHeight = 126f + rowCount * RowHeight;
            Rect view = new Rect(0f, 0f, content.width - 16f, viewHeight);
            Widgets.BeginScrollView(content, ref scroll, view);

            float y = 0f;
            DrawColonistTable(new Rect(0f, y, view.width, viewHeight - y), ref y, out colonistClicked, out selectedColonist);
            y += 12f;
            DrawWorkGiverTable(new Rect(0f, y, view.width, viewHeight - y), ref y);

            Widgets.EndScrollView();

            if (pendingGroupSelection != null)
            {
                SetGroup(pendingGroupSelection);
                groupChanged = pendingGroupSelection;
                pendingGroupSelection = null;
            }
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

            int totalTravel = 0;
            int totalWork = 0;
            foreach (ColonistWorkStat colonist in stats.ColonistStats)
            {
                totalTravel += colonist.TravelTicksSpent;
                totalWork += colonist.WorkTicksSpent;
            }

            string totals = "WorkMonitor.JobsWorkWalkActiveSummary".Translate(
                stats.TotalJobCount,
                WorkMonitorUtility.FormatWorkUnits(stats.TotalWorkUnits),
                WorkMonitorUtility.FormatDuration(totalTravel, WorkMonitorMod.Settings?.showTimeInHours ?? true),
                WorkMonitorUtility.FormatDuration(totalWork, WorkMonitorMod.Settings?.showTimeInHours ?? true));
            Widgets.Label(new Rect(rect.x + 14f, rect.y + 18f, rect.width - 14f, 16f), totals);

            string mapSummary = "WorkMonitor.MapSummary".Translate(
                stats.TotalMapOpenTasks,
                WorkMonitorUtility.FormatWorkUnits(stats.TotalMapWorkLeft),
                WorkMonitorUtility.FormatGameDateTime(stats.MapSampleTick),
                WorkMonitorUtility.FormatSampleAge(stats.MapSampleTick));
            Widgets.Label(new Rect(rect.x + 14f, rect.y + 36f, rect.width - 14f, 16f), mapSummary);
        }

        private void DrawColonistTable(Rect area, ref float y, out bool colonistClicked, out ColonistWorkStat selectedColonist)
        {
            colonistClicked = false;
            selectedColonist = null;

            Text.Font = GameFont.Small;
            Rect titleRect = new Rect(area.x, y, area.width, 22f);
            Widgets.Label(titleRect, "WorkMonitor.Colonists".Translate());
            TooltipHandler.TipRegion(titleRect, "WorkMonitor.ColonistsIconTip".Translate());
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

                GetColonistTableColumns(
                    row,
                    out Rect iconsCol,
                    out Rect labelCol,
                    out _,
                    out _,
                    out _,
                    out _,
                    out _,
                    out _);

                Rect clickRect = new Rect(labelCol.x, row.y, row.xMax - labelCol.x, row.height);
                if (Widgets.ButtonInvisible(clickRect))
                {
                    colonistClicked = true;
                    selectedColonist = colonist;
                }

                if (DrawColonistRow(row, colonist, iconsCol, out bool inspectClicked) && inspectClicked)
                {
                    ColonistInspectUtility.OpenPawnProfile(colonist.Pawn);
                }

                y += RowHeight;
                rowIndex++;
            }
        }

        private void DrawWorkGiverTable(Rect area, ref float y)
        {
            Text.Font = GameFont.Small;
            Rect titleRect = new Rect(area.x, y, area.width, 22f);
            Widgets.Label(titleRect, "WorkMonitor.WorkGiversMap".Translate());
            TooltipHandler.TipRegion(titleRect, "WorkMonitor.WorkGiversMapTip".Translate());
            y += RowHeight;

            Text.Font = GameFont.Tiny;
            Color prevColor = GUI.color;
            GUI.color = new Color(0.72f, 0.72f, 0.72f);
            Widgets.Label(
                new Rect(area.x, y, area.width, 16f),
                "WorkMonitor.MapSampleGameTime".Translate(
                    WorkMonitorUtility.FormatGameDateTime(stats.MapSampleTick),
                    WorkMonitorUtility.FormatSampleAge(stats.MapSampleTick)));
            GUI.color = prevColor;
            y += 18f;

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

            Rect totalRow = new Rect(area.x, y, area.width, RowHeight);
            Widgets.DrawBoxSolid(totalRow, new Color(1f, 1f, 1f, 0.05f));
            DrawWorkGiverTotalRow(totalRow);
            y += RowHeight;
        }

        private static void DrawColonistHeader(Rect row)
        {
            Text.Font = GameFont.Tiny;
            Color prev = GUI.color;
            GUI.color = new Color(0.72f, 0.72f, 0.72f);

            GetColonistTableColumns(
                row,
                out Rect iconsCol,
                out Rect labelCol,
                out Rect kpiJobCol,
                out Rect kpiWorkCol,
                out Rect jobsCol,
                out Rect workCol,
                out Rect walkCol,
                out Rect activeWorkCol);

            Widgets.Label(iconsCol, "");
            Widgets.Label(labelCol, "WorkMonitor.Colonist".Translate());
            LabelRight(kpiJobCol, "WorkMonitor.KpiJobs".Translate());
            LabelRight(kpiWorkCol, "WorkMonitor.KpiWork".Translate());
            LabelRight(jobsCol, "WorkMonitor.Jobs".Translate());
            LabelRight(workCol, "WorkMonitor.Work".Translate());
            LabelRight(walkCol, "WorkMonitor.Walk".Translate());
            LabelRight(activeWorkCol, "WorkMonitor.WorkTime".Translate());

            GUI.color = prev;
        }

        private static bool DrawColonistRow(Rect row, ColonistWorkStat colonist, Rect iconsCol, out bool inspectClicked)
        {
            inspectClicked = false;

            Text.Font = GameFont.Small;
            GetColonistTableColumns(
                row,
                out _,
                out Rect labelCol,
                out Rect kpiJobCol,
                out Rect kpiWorkCol,
                out Rect jobsCol,
                out Rect workCol,
                out Rect walkCol,
                out Rect activeWorkCol);

            Rect inspectRect = new Rect(iconsCol.x, row.y + (row.height - ColonistIconSize) * 0.5f, ColonistIconSize, ColonistIconSize);
            if (Widgets.ButtonImage(inspectRect, TexButton.Info))
            {
                inspectClicked = true;
                return true;
            }

            TooltipHandler.TipRegion(inspectRect, "WorkMonitor.OpenColonistProfile".Translate());

            string passion = WorkMonitorUiUtility.PassionShort(colonist.Passion);
            Widgets.Label(labelCol, (passion + " " + colonist.Label).Trim().Truncate(labelCol.width));
            LabelRight(kpiJobCol, FormatPerHour(colonist.JobsPerHour, integer: true));
            LabelRight(kpiWorkCol, FormatPerHour(colonist.WorkUnitsPerHour, integer: false));
            LabelRight(jobsCol, colonist.JobCount.ToString());
            LabelRight(workCol, WorkMonitorUtility.FormatWorkUnits(colonist.WorkUnitsSpent));
            LabelRight(
                walkCol,
                WorkMonitorUtility.FormatDuration(colonist.TravelTicksSpent, WorkMonitorMod.Settings?.showTimeInHours ?? true));
            LabelRight(
                activeWorkCol,
                WorkMonitorUtility.FormatDuration(colonist.WorkTicksSpent, WorkMonitorMod.Settings?.showTimeInHours ?? true));
            return false;
        }

        private static void DrawWorkGiverHeader(Rect row)
        {
            Text.Font = GameFont.Tiny;
            Color prev = GUI.color;
            GUI.color = new Color(0.72f, 0.72f, 0.72f);

            GetMapTableColumns(row, out Rect labelCol, out Rect jobsCol, out Rect workCol);

            Widgets.Label(labelCol, "WorkMonitor.WorkGiver".Translate());
            LabelRight(jobsCol, "WorkMonitor.Jobs".Translate());
            LabelRight(workCol, "WorkMonitor.Work".Translate());

            GUI.color = prev;
        }

        private void DrawWorkGiverRow(Rect row, WorkGiverStat wg)
        {
            Text.Font = GameFont.Small;
            GetMapTableColumns(row, out Rect labelCol, out Rect jobsCol, out Rect workCol);

            Widgets.Label(labelCol, wg.Label.Truncate(labelCol.width));
            LabelRight(jobsCol, wg.JobCount.ToString());
            LabelRight(workCol, WorkMonitorUtility.FormatWorkUnits(wg.WorkUnitsSpent));
        }

        private void DrawWorkGiverTotalRow(Rect row)
        {
            Text.Font = GameFont.Small;
            GetMapTableColumns(row, out Rect labelCol, out Rect jobsCol, out Rect workCol);

            Color prev = GUI.color;
            GUI.color = new Color(0.85f, 0.85f, 0.85f);
            Widgets.Label(labelCol, "WorkMonitor.MapTotal".Translate(stats.Group.Label));
            LabelRight(jobsCol, stats.TotalMapOpenTasks.ToString());
            LabelRight(workCol, WorkMonitorUtility.FormatWorkUnits(stats.TotalMapWorkLeft));
            GUI.color = prev;
        }

        private static void GetMapTableColumns(Rect row, out Rect labelCol, out Rect jobsCol, out Rect workCol)
        {
            float workX = row.xMax - WorkWidth;
            float jobsX = workX - ColumnGap - JobsWidth;
            float labelX = row.x;
            float labelWidth = jobsX - ColumnGap - labelX;

            labelCol = new Rect(labelX, row.y, Mathf.Max(labelWidth, 60f), row.height);
            jobsCol = new Rect(jobsX, row.y, JobsWidth, row.height);
            workCol = new Rect(workX, row.y, WorkWidth, row.height);
        }

        private static void GetColonistTableColumns(
            Rect row,
            out Rect iconsCol,
            out Rect labelCol,
            out Rect kpiJobCol,
            out Rect kpiWorkCol,
            out Rect jobsCol,
            out Rect workCol,
            out Rect walkCol,
            out Rect activeWorkCol)
        {
            float activeWorkX = row.xMax - ActiveWorkWidth;
            float walkX = activeWorkX - ColumnGap - WalkWidth;
            float workX = walkX - ColumnGap - WorkWidth;
            float jobsX = workX - ColumnGap - JobsWidth;
            float kpiWorkX = jobsX - ColumnGap - KpiWorkWidth;
            float kpiJobX = kpiWorkX - ColumnGap - KpiJobWidth;
            float labelX = row.x + ColonistIconsWidth + 4f;
            float labelWidth = kpiJobX - ColumnGap - labelX;

            iconsCol = new Rect(row.x, row.y, ColonistIconsWidth, row.height);
            labelCol = new Rect(labelX, row.y, Mathf.Max(labelWidth, 48f), row.height);
            kpiJobCol = new Rect(kpiJobX, row.y, KpiJobWidth, row.height);
            kpiWorkCol = new Rect(kpiWorkX, row.y, KpiWorkWidth, row.height);
            jobsCol = new Rect(jobsX, row.y, JobsWidth, row.height);
            workCol = new Rect(workX, row.y, WorkWidth, row.height);
            walkCol = new Rect(walkX, row.y, WalkWidth, row.height);
            activeWorkCol = new Rect(activeWorkX, row.y, ActiveWorkWidth, row.height);
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
