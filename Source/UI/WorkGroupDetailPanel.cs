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
        private const float EndlessJobWidth = 44f;
        private const float WorkWidth = 52f;
        private const float WalkWidth = 40f;
        private const float ActiveWorkWidth = 40f;
        private const float ColumnGap = 10f;
        private const float GroupDropdownWidth = 160f;
        private const float ExpandButtonWidth = 20f;
        private const float WorkGiverIndent = 16f;

        private readonly WorkGroupChartPanel chartPanel = new WorkGroupChartPanel();
        private Vector2 scroll;
        private WorkGroupStats stats;
        private List<WorkGroupStats> allStats = new List<WorkGroupStats>();
        private WorkGroupSnapshot pendingGroupSelection;
        private MonitorRangeState boundRangeState;
        private readonly HashSet<int> expandedColonistIds = new HashSet<int>();
        private readonly Dictionary<int, ColonistGroupWorkDetail> colonistDetailCache = new Dictionary<int, ColonistGroupWorkDetail>();

        public WorkGroupSnapshot CurrentGroup => stats?.Group;

        public void SetGroup(WorkGroupSnapshot group, MonitorRangeState rangeState)
        {
            boundRangeState = rangeState;
            allStats = WorkGroupStatsAggregator.BuildAll(rangeState.RangeHours);
            stats = WorkGroupStatsAggregator.Build(group, rangeState.RangeHours);
            expandedColonistIds.Clear();
            colonistDetailCache.Clear();
        }

        public void Draw(Rect rect, MonitorRangeState rangeState, out bool back, out bool colonistClicked, out ColonistWorkStat selectedColonist, out WorkGroupSnapshot groupChanged, out bool workGiverClicked, out WorkGiverDef selectedWorkGiver)
        {
            back = false;
            colonistClicked = false;
            selectedColonist = null;
            groupChanged = null;
            workGiverClicked = false;
            selectedWorkGiver = null;

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

            float toolbarX = dropdownRect.xMax + 6f;
            Text.Font = GameFont.Tiny;
            WorkMonitorDropdownUtility.DrawRangeDropdown(
                new Rect(toolbarX, rect.y, 110f, 26f),
                rangeState,
                () => SetGroup(stats.Group, rangeState));

            if (Widgets.ButtonText(new Rect(toolbarX + 100f, rect.y, 80f, 26f), "WorkMonitor.Refresh".Translate()))
            {
                SetGroup(stats.Group, rangeState);
            }

            Rect header = new Rect(rect.x, rect.y + 32f, rect.width, 52f);
            DrawHeader(header);

            Rect chartRect = new Rect(rect.x, header.yMax + 4f, rect.width, ChartHeight);
            chartPanel.Draw(chartRect, stats, allStats, rangeState, () => SetGroup(stats.Group, rangeState));

            Rect content = new Rect(rect.x, chartRect.yMax + 6f, rect.width, rect.yMax - chartRect.yMax - 12f);
            float viewHeight = 126f + CalculateColonistViewHeight() + stats.WorkGiverStats.Count * RowHeight;
            Rect view = new Rect(0f, 0f, content.width - 16f, viewHeight);
            Widgets.BeginScrollView(content, ref scroll, view);

            float y = 0f;
            DrawColonistTable(new Rect(0f, y, view.width, viewHeight - y), rangeState, ref y, out colonistClicked, out selectedColonist, out workGiverClicked, out selectedWorkGiver);
            y += 12f;
            DrawWorkGiverTable(new Rect(0f, y, view.width, viewHeight - y), ref y, out bool mapWgClicked, out WorkGiverDef mapWg);
            if (mapWgClicked)
            {
                workGiverClicked = true;
                selectedWorkGiver = mapWg;
            }

            Widgets.EndScrollView();

            if (pendingGroupSelection != null)
            {
                SetGroup(pendingGroupSelection, rangeState);
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
                WorkMonitorUiUtility.FormatMapOpenTasks(stats.TotalMapOpenTasks, stats.TotalMapNewTodayOpenTasks),
                WorkMonitorUiUtility.FormatMapWorkLeft(stats.TotalMapWorkLeft, stats.TotalMapNewTodayWorkLeft),
                WorkMonitorUtility.FormatGameDateTime(stats.MapSampleTick),
                WorkMonitorUtility.FormatSampleAge(stats.MapSampleTick));
            Widgets.Label(new Rect(rect.x + 14f, rect.y + 36f, rect.width - 14f, 16f), mapSummary);
        }

        private float CalculateColonistViewHeight()
        {
            float height = stats.ColonistStats.Count * RowHeight;
            foreach (ColonistWorkStat colonist in stats.ColonistStats)
            {
                if (expandedColonistIds.Contains(colonist.Pawn.thingIDNumber))
                {
                    ColonistGroupWorkDetail detail = GetColonistDetail(colonist.Pawn);
                    height += detail.WorkGiverStats.Count * RowHeight;
                }
            }

            return height;
        }

        private ColonistGroupWorkDetail GetColonistDetail(Pawn pawn)
        {
            int id = pawn.thingIDNumber;
            if (!colonistDetailCache.TryGetValue(id, out ColonistGroupWorkDetail detail))
            {
                detail = ColonistStatsAggregator.BuildGroupDetail(pawn, stats.Group, boundRangeState?.RangeHours ?? 24);
                colonistDetailCache[id] = detail;
            }

            return detail;
        }

        private void DrawColonistTable(Rect area, MonitorRangeState rangeState, ref float y, out bool colonistClicked, out ColonistWorkStat selectedColonist, out bool workGiverClicked, out WorkGiverDef selectedWorkGiver)
        {
            colonistClicked = false;
            selectedColonist = null;
            workGiverClicked = false;
            selectedWorkGiver = null;

            Text.Font = GameFont.Small;
            const float expandAllWidth = 76f;
            Rect titleRect = new Rect(area.x, y, area.width - expandAllWidth - 4f, 22f);
            Widgets.Label(titleRect, "WorkMonitor.Colonists".Translate());
            TooltipHandler.TipRegion(titleRect, "WorkMonitor.ColonistsTip".Translate());
            string expandLabel = AllColonistsExpanded()
                ? "WorkMonitor.CollapseAll".Translate()
                : "WorkMonitor.ExpandAll".Translate();
            if (Widgets.ButtonText(new Rect(area.xMax - expandAllWidth, y, expandAllWidth, 22f), expandLabel))
            {
                ToggleExpandAllColonists();
            }
            y += RowHeight;

            DrawColonistHeader(new Rect(area.x, y, area.width, RowHeight));
            y += RowHeight;

            int rowIndex = 0;
            foreach (ColonistWorkStat colonist in stats.ColonistStats)
            {
                int pawnId = colonist.Pawn.thingIDNumber;
                bool expanded = expandedColonistIds.Contains(pawnId);

                Rect row = new Rect(area.x, y, area.width, RowHeight);
                if (rowIndex % 2 == 1)
                {
                    Widgets.DrawBoxSolid(row, new Color(1f, 1f, 1f, 0.03f));
                }

                Rect expandRect = new Rect(row.x, row.y, ExpandButtonWidth, row.height);
                if (Widgets.ButtonText(expandRect, expanded ? "▼" : "▶"))
                {
                    if (expanded)
                    {
                        expandedColonistIds.Remove(pawnId);
                    }
                    else
                    {
                        expandedColonistIds.Add(pawnId);
                    }
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

                if (expanded)
                {
                    DrawExpandedWorkGivers(area, colonist.Pawn, rangeState, ref y, ref rowIndex, out bool wgClicked, out WorkGiverDef wg);
                    if (wgClicked)
                    {
                        workGiverClicked = true;
                        selectedWorkGiver = wg;
                    }
                }
            }
        }

        private bool AllColonistsExpanded()
        {
            if (stats.ColonistStats.Count == 0)
            {
                return true;
            }

            foreach (ColonistWorkStat colonist in stats.ColonistStats)
            {
                if (!expandedColonistIds.Contains(colonist.Pawn.thingIDNumber))
                {
                    return false;
                }
            }

            return true;
        }

        private void ToggleExpandAllColonists()
        {
            if (AllColonistsExpanded())
            {
                expandedColonistIds.Clear();
                return;
            }

            foreach (ColonistWorkStat colonist in stats.ColonistStats)
            {
                expandedColonistIds.Add(colonist.Pawn.thingIDNumber);
            }
        }

        private void DrawExpandedWorkGivers(Rect area, Pawn pawn, MonitorRangeState rangeState, ref float y, ref int rowIndex, out bool workGiverClicked, out WorkGiverDef selectedWorkGiver)
        {
            workGiverClicked = false;
            selectedWorkGiver = null;
            ColonistGroupWorkDetail detail = GetColonistDetail(pawn);
            bool showHours = WorkMonitorMod.Settings?.showTimeInHours ?? true;

            foreach (ColonistWorkGiverStat wg in detail.WorkGiverStats)
            {
                Rect row = new Rect(area.x + WorkGiverIndent, y, area.width - WorkGiverIndent, RowHeight);
                if (rowIndex % 2 == 1)
                {
                    Widgets.DrawBoxSolid(row, new Color(1f, 1f, 1f, 0.02f));
                }

                if (Widgets.ButtonInvisible(row))
                {
                    workGiverClicked = true;
                    selectedWorkGiver = wg.WorkGiver;
                }

                Rect columnRow = new Rect(area.x, y, area.width, RowHeight);
                float metricsLeft = WorkMonitorTableColumns.ColonistWorkGiverMetricsLeftEdge(columnRow);
                Text.Font = GameFont.Tiny;
                Color prev = GUI.color;
                GUI.color = new Color(0.8f, 0.8f, 0.8f);
                Widgets.Label(new Rect(area.x + WorkGiverIndent, row.y, metricsLeft - area.x - WorkGiverIndent - 8f, row.height), wg.Label.Truncate(metricsLeft - area.x - WorkGiverIndent - 8f));
                GUI.color = prev;
                Text.Font = GameFont.Small;

                WorkMonitorTableColumns.GetColonistWorkGiverColumns(columnRow, out Rect jobCol, out Rect endlessCol, out Rect workCol, out Rect walkCol, out Rect activeWorkCol, out _);
                LabelRight(jobCol, wg.JobCount.ToString());
                LabelRight(endlessCol, wg.EndlessJobCount.ToString());
                LabelRight(workCol, WorkMonitorUtility.FormatWorkUnits(wg.WorkUnitsSpent));
                LabelRight(walkCol, WorkMonitorUtility.FormatDuration(wg.TravelTicksSpent, showHours));
                LabelRight(activeWorkCol, WorkMonitorUtility.FormatDuration(wg.WorkTicksSpent, showHours));

                y += RowHeight;
                rowIndex++;
            }
        }

        private void DrawWorkGiverTable(Rect area, ref float y, out bool workGiverClicked, out WorkGiverDef selectedWorkGiver)
        {
            workGiverClicked = false;
            selectedWorkGiver = null;
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

                DrawWorkGiverRow(row, wg, out bool clicked);
                if (clicked)
                {
                    workGiverClicked = true;
                    selectedWorkGiver = wg.WorkGiver;
                }
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
                out Rect endlessCol,
                out Rect workCol,
                out Rect walkCol,
                out Rect activeWorkCol);

            Widgets.Label(new Rect(row.x, row.y, ExpandButtonWidth, row.height), "");
            Widgets.Label(labelCol, "WorkMonitor.Colonist".Translate());
            Widgets.Label(iconsCol, "");
            LabelRight(kpiJobCol, "WorkMonitor.KpiJobs".Translate());
            LabelRight(kpiWorkCol, "WorkMonitor.KpiWork".Translate());
            LabelRight(jobsCol, "WorkMonitor.Jobs".Translate());
            Rect endlessHeader = endlessCol;
            LabelRight(endlessHeader, "WorkMonitor.EndlessJobs".Translate());
            TooltipHandler.TipRegion(endlessHeader, "WorkMonitor.EndlessJobsTip".Translate());
            LabelRight(workCol, "WorkMonitor.Work".Translate());
            TooltipHandler.TipRegion(workCol, "WorkMonitor.WorkEstimatedTip".Translate());
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
                out Rect endlessCol,
                out Rect workCol,
                out Rect walkCol,
                out Rect activeWorkCol);

            string passion = WorkMonitorUiUtility.PassionShort(colonist.Passion);
            Widgets.Label(labelCol, (passion + " " + colonist.Label).Trim().Truncate(labelCol.width));

            Rect inspectRect = new Rect(iconsCol.x, row.y + (row.height - ColonistIconSize) * 0.5f, ColonistIconSize, ColonistIconSize);
            if (Widgets.ButtonImage(inspectRect, TexButton.Info))
            {
                inspectClicked = true;
                return true;
            }

            TooltipHandler.TipRegion(inspectRect, "WorkMonitor.OpenColonistProfile".Translate());

            LabelRight(kpiJobCol, FormatPerHour(colonist.JobsPerHour, integer: true));
            LabelRight(kpiWorkCol, FormatPerHour(colonist.WorkUnitsPerHour, integer: false));
            LabelRight(jobsCol, colonist.JobCount.ToString());
            LabelRight(endlessCol, colonist.EndlessJobCount.ToString());
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

            GetMapTableColumns(row, out Rect labelCol, out Rect mapJobsCol, out Rect mapWorkCol, out Rect colonistJobsCol, out Rect endlessCol);

            Widgets.Label(labelCol, "WorkMonitor.WorkGiver".Translate());
            LabelRight(mapJobsCol, "WorkMonitor.ExistJob".Translate());
            TooltipHandler.TipRegion(mapJobsCol, "WorkMonitor.ExistJobTip".Translate());
            LabelRight(mapWorkCol, "WorkMonitor.ExistWork".Translate());
            TooltipHandler.TipRegion(mapWorkCol, "WorkMonitor.ExistWorkTip".Translate());
            LabelRight(colonistJobsCol, "WorkMonitor.Jobs".Translate());
            TooltipHandler.TipRegion(colonistJobsCol, "WorkMonitor.JobProcessedTip".Translate());
            Rect endlessHeader = endlessCol;
            LabelRight(endlessHeader, "WorkMonitor.EndlessJobs".Translate());
            TooltipHandler.TipRegion(endlessHeader, "WorkMonitor.EndlessJobsTip".Translate());

            GUI.color = prev;
        }

        private void DrawWorkGiverRow(Rect row, WorkGiverStat wg, out bool clicked)
        {
            clicked = false;
            Text.Font = GameFont.Small;
            GetMapTableColumns(row, out Rect labelCol, out Rect mapJobsCol, out Rect mapWorkCol, out Rect colonistJobsCol, out Rect endlessCol);

            if (Widgets.ButtonInvisible(row))
            {
                clicked = true;
            }

            Widgets.Label(labelCol, wg.Label.Truncate(labelCol.width));
            string mapJobsText = WorkMonitorUiUtility.FormatMapOpenTasks(wg.MapOpenTasks, wg.MapNewTodayOpenTasks);
            string mapWorkText = WorkMonitorUiUtility.FormatMapWorkLeft(wg.MapWorkLeft, wg.MapNewTodayWorkLeft);
            LabelRight(mapJobsCol, mapJobsText);
            TooltipHandler.TipRegion(mapJobsCol, "WorkMonitor.MapJobsNewTodayTip".Translate(mapJobsText));
            LabelRight(mapWorkCol, mapWorkText);
            TooltipHandler.TipRegion(mapWorkCol, "WorkMonitor.MapWorkNewTodayTip".Translate(mapWorkText));
            LabelRight(colonistJobsCol, wg.JobCount.ToString());
            LabelRight(endlessCol, wg.EndlessJobCount.ToString());
        }

        private void DrawWorkGiverTotalRow(Rect row)
        {
            Text.Font = GameFont.Small;
            GetMapTableColumns(row, out Rect labelCol, out Rect mapJobsCol, out Rect mapWorkCol, out Rect colonistJobsCol, out Rect endlessCol);

            Color prev = GUI.color;
            GUI.color = new Color(0.85f, 0.85f, 0.85f);
            Widgets.Label(labelCol, "WorkMonitor.MapTotal".Translate(stats.Group.Label));
            string mapJobsText = WorkMonitorUiUtility.FormatMapOpenTasks(stats.TotalMapOpenTasks, stats.TotalMapNewTodayOpenTasks);
            string mapWorkText = WorkMonitorUiUtility.FormatMapWorkLeft(stats.TotalMapWorkLeft, stats.TotalMapNewTodayWorkLeft);
            LabelRight(mapJobsCol, mapJobsText);
            LabelRight(mapWorkCol, mapWorkText);
            LabelRight(colonistJobsCol, stats.TotalJobCount.ToString());
            LabelRight(endlessCol, stats.TotalEndlessJobCount.ToString());
            GUI.color = prev;
        }

        private static void GetMapTableColumns(Rect row, out Rect labelCol, out Rect mapJobsCol, out Rect mapWorkCol, out Rect colonistJobsCol, out Rect endlessCol)
        {
            float endlessX = row.xMax - EndlessJobWidth;
            float colonistJobsX = endlessX - ColumnGap - JobsWidth;
            float mapWorkX = colonistJobsX - ColumnGap - WorkWidth;
            float mapJobsX = mapWorkX - ColumnGap - JobsWidth;
            float labelX = row.x;
            float labelWidth = mapJobsX - ColumnGap - labelX;

            labelCol = new Rect(labelX, row.y, Mathf.Max(labelWidth, 60f), row.height);
            mapJobsCol = new Rect(mapJobsX, row.y, JobsWidth, row.height);
            mapWorkCol = new Rect(mapWorkX, row.y, WorkWidth, row.height);
            colonistJobsCol = new Rect(colonistJobsX, row.y, JobsWidth, row.height);
            endlessCol = new Rect(endlessX, row.y, EndlessJobWidth, row.height);
        }

        private static void GetColonistTableColumns(
            Rect row,
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
            float activeWorkX = row.xMax - ActiveWorkWidth;
            float walkX = activeWorkX - ColumnGap - WalkWidth;
            float workX = walkX - ColumnGap - WorkWidth;
            float endlessX = workX - ColumnGap - EndlessJobWidth;
            float jobsX = endlessX - ColumnGap - JobsWidth;
            float kpiWorkX = jobsX - ColumnGap - KpiWorkWidth;
            float kpiJobX = kpiWorkX - ColumnGap - KpiJobWidth;
            float iconX = kpiJobX - ColumnGap - ColonistIconSize;
            float labelX = row.x + ExpandButtonWidth + 4f;
            float labelWidth = iconX - ColumnGap - labelX;

            labelCol = new Rect(labelX, row.y, Mathf.Max(labelWidth, 48f), row.height);
            iconsCol = new Rect(iconX, row.y, ColonistIconSize, row.height);
            kpiJobCol = new Rect(kpiJobX, row.y, KpiJobWidth, row.height);
            kpiWorkCol = new Rect(kpiWorkX, row.y, KpiWorkWidth, row.height);
            jobsCol = new Rect(jobsX, row.y, JobsWidth, row.height);
            endlessCol = new Rect(endlessX, row.y, EndlessJobWidth, row.height);
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
