using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using WorkMonitor.Groups;

namespace WorkMonitor.UI
{
    public class WorkGroupDetailPanel
    {
        private const float RowHeight = 24f;
        private const float HeaderHeight = 18f;
        private const float ChartHeight = 168f;
        private const float MapPieHeight = 168f;
        private const float MapPieGap = 12f;
        private const float ColonistIconSize = WorkMonitorTableColumns.ColonistIconSize;
        private const float JobsWidth = 63f;
        private const float EndlessJobWidth = 66f;
        private const float WorkWidth = 78f;
        private const float ColumnGap = 10f;
        private const float GroupDropdownWidth = 160f;
        private const float ExpandButtonWidth = WorkMonitorTableColumns.ExpandButtonWidth;
        private const float WorkGiverIndent = 16f;
        private const float ExpandAllWidth = 76f;
        private const float LayoutToggleWidth = 96f;
        private const float ToolbarGap = 4f;

        private readonly WorkGroupChartPanel chartPanel = new WorkGroupChartPanel();
        private readonly WorkGroupMapBacklogPieChartPanel mapPiePanel = new WorkGroupMapBacklogPieChartPanel();
        private Vector2 scroll;
        private WorkGroupStats stats;
        private List<WorkGroupStats> allStats = new List<WorkGroupStats>();
        private WorkGroupSnapshot pendingGroupSelection;
        private MonitorRangeState boundRangeState;
        private readonly HashSet<int> expandedColonistIds = new HashSet<int>();
        private readonly HashSet<string> expandedWorkGiverDefNames = new HashSet<string>();
        private readonly Dictionary<int, ColonistGroupWorkDetail> colonistDetailCache = new Dictionary<int, ColonistGroupWorkDetail>();
        private readonly Dictionary<string, WorkGiverDetailStats> workGiverDetailCache = new Dictionary<string, WorkGiverDetailStats>();
        private List<WorkGiverDef> activeWorkGivers;

        private static bool WorkGiverFirst => WorkMonitorMod.Settings?.WorkGiverFirst ?? false;

        public WorkGroupSnapshot CurrentGroup => stats?.Group;

        public void SetGroup(WorkGroupSnapshot group, MonitorRangeState rangeState)
        {
            boundRangeState = rangeState;
            allStats = WorkGroupStatsAggregator.BuildAll(rangeState.RangeHours);
            stats = WorkGroupStatsAggregator.Build(group, rangeState.RangeHours);
            ClearExpandCaches();
        }

        private void ClearExpandCaches()
        {
            expandedColonistIds.Clear();
            expandedWorkGiverDefNames.Clear();
            colonistDetailCache.Clear();
            workGiverDetailCache.Clear();
            activeWorkGivers = null;
        }

        private void ToggleLayout()
        {
            WorkMonitorSettings settings = WorkMonitorMod.Settings;
            if (settings == null)
            {
                return;
            }

            settings.overviewLayoutMode = settings.WorkGiverFirst
                ? OverviewLayoutMode.WorkTypeColonistFirst
                : OverviewLayoutMode.WorkTypeWorkGiverFirst;
            ClearExpandCaches();
            if (stats != null && boundRangeState != null)
            {
                SetGroup(stats.Group, boundRangeState);
            }
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

            Rect content = new Rect(rect.x, rect.y + 32f, rect.width, rect.yMax - rect.y - 38f);
            float viewHeight = CalculateScrollViewHeight();
            Rect view = new Rect(0f, 0f, content.width - 16f, viewHeight);
            Widgets.BeginScrollView(content, ref scroll, view);

            float y = 0f;
            DrawHeader(new Rect(0f, y, view.width, HeaderHeight));
            y += HeaderHeight + 4f;

            chartPanel.Draw(new Rect(0f, y, view.width, ChartHeight), stats, allStats, rangeState);
            y += ChartHeight + 6f;

            DrawColonistTable(new Rect(0f, y, view.width, viewHeight - y), rangeState, ref y, out colonistClicked, out selectedColonist, out workGiverClicked, out selectedWorkGiver);
            y += MapPieGap;
            mapPiePanel.Draw(new Rect(0f, y, view.width, MapPieHeight), stats);
            y += MapPieHeight + MapPieGap;
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

        private float CalculateScrollViewHeight()
        {
            return HeaderHeight + 4f + ChartHeight + 6f
                + 150f + CalculateColonistViewHeight() + MapPieGap + MapPieHeight + MapPieGap
                + stats.WorkGiverStats.Count * RowHeight + RowHeight;
        }

        private void DrawHeader(Rect rect)
        {
            Color dot = WorkMonitorUiUtility.StatusColor(stats.Status);
            Widgets.DrawBoxSolid(new Rect(rect.x, rect.y + 3f, 10f, 10f), dot);

            Text.Font = GameFont.Tiny;
            int totalTravel = 0;
            int totalWork = 0;
            foreach (ColonistWorkStat colonist in stats.ColonistStats)
            {
                totalTravel += colonist.TravelTicksSpent;
                totalWork += colonist.WorkTicksSpent;
            }

            bool showHours = WorkMonitorMod.Settings?.showTimeInHours ?? true;
            string summary = "WorkMonitor.GroupDetailSummary".Translate(
                stats.CapableCount,
                stats.EnabledCount,
                stats.WorkedCount,
                WorkMonitorUiUtility.FormatInterestRatio(stats),
                stats.TotalJobCount,
                WorkMonitorUtility.FormatWorkUnits(stats.TotalWorkUnits),
                WorkMonitorUtility.FormatDuration(totalTravel, showHours),
                WorkMonitorUtility.FormatDuration(totalWork, showHours),
                WorkMonitorUiUtility.FormatMapOpenTasks(stats.TotalMapOpenTasks, stats.TotalMapNewTodayOpenTasks),
                WorkMonitorUiUtility.FormatMapWorkLeft(stats.TotalMapWorkLeft, stats.TotalMapNewTodayWorkLeft),
                WorkMonitorUtility.FormatSampleAge(stats.MapSampleTick));
            Widgets.Label(new Rect(rect.x + 14f, rect.y, rect.width - 14f, rect.height), summary);
        }

        private float CalculateColonistViewHeight()
        {
            if (WorkGiverFirst)
            {
                float height = GetActiveWorkGivers().Count * RowHeight;
                foreach (WorkGiverDef workGiver in GetActiveWorkGivers())
                {
                    if (expandedWorkGiverDefNames.Contains(workGiver.defName))
                    {
                        WorkGiverDetailStats detail = GetWorkGiverDetail(workGiver);
                        height += detail.ColonistStats.Count * RowHeight;
                    }
                }

                return height;
            }

            float colonistHeight = stats.ColonistStats.Count * RowHeight;
            if (HasMapOnlyBacklog())
            {
                colonistHeight += RowHeight;
            }

            foreach (ColonistWorkStat colonist in stats.ColonistStats)
            {
                if (expandedColonistIds.Contains(colonist.PawnId))
                {
                    ColonistGroupWorkDetail detail = GetColonistDetail(colonist.PawnId);
                    colonistHeight += detail.WorkGiverStats.Count * RowHeight;
                }
            }

            if (expandedColonistIds.Contains(BulkExpandUtility.UnassignedBacklogPawnId))
            {
                colonistHeight += GetMapOnlyWorkGivers().Count * RowHeight;
            }

            return colonistHeight;
        }

        private WorkGiverDetailStats GetWorkGiverDetail(WorkGiverDef workGiver)
        {
            if (workGiver == null)
            {
                return null;
            }

            if (!workGiverDetailCache.TryGetValue(workGiver.defName, out WorkGiverDetailStats detail))
            {
                detail = WorkGiverStatsAggregator.Build(stats.Group, workGiver, boundRangeState?.RangeHours ?? 24);
                workGiverDetailCache[workGiver.defName] = detail;
            }

            return detail;
        }

        private List<WorkGiverDef> GetActiveWorkGivers()
        {
            if (activeWorkGivers != null)
            {
                return activeWorkGivers;
            }

            var ranked = new List<(WorkGiverDef workGiver, int ticks, float mapWork, int mapOpen)>();
            foreach (WorkGiverDef workGiver in stats.Group.WorkGivers)
            {
                WorkGiverDetailStats detail = GetWorkGiverDetail(workGiver);
                WorkGiverStat mapStat = stats.WorkGiverStats.Find(wg => wg.WorkGiver == workGiver);
                if (!BulkExpandUtility.IsVisibleWorkGiverRow(detail, mapStat))
                {
                    continue;
                }

                ranked.Add((
                    workGiver,
                    BulkExpandUtility.RankTicks(detail),
                    BulkExpandUtility.RankMapWork(mapStat),
                    BulkExpandUtility.RankMapOpenTasks(mapStat)));
            }

            activeWorkGivers = ranked
                .OrderByDescending(entry => entry.ticks)
                .ThenByDescending(entry => entry.mapWork)
                .ThenByDescending(entry => entry.mapOpen)
                .Select(entry => entry.workGiver)
                .ToList();
            return activeWorkGivers;
        }

        private ColonistGroupWorkDetail GetColonistDetail(int pawnId)
        {
            if (!colonistDetailCache.TryGetValue(pawnId, out ColonistGroupWorkDetail detail))
            {
                detail = ColonistStatsAggregator.BuildGroupDetail(pawnId, stats.Group, boundRangeState?.RangeHours ?? 24);
                colonistDetailCache[pawnId] = detail;
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
            float tableWidth = area.width;
            float expandCollapseWidth = ExpandAllWidth * 2f + ToolbarGap;
            float toolbarWidth = LayoutToggleWidth + expandCollapseWidth + 4f;

            string layoutLabel = WorkGiverFirst
                ? "WorkMonitor.GroupByWorkGiver".Translate()
                : "WorkMonitor.GroupByColonist".Translate();
            Rect layoutRect = new Rect(area.xMax - toolbarWidth, y, LayoutToggleWidth, 22f);
            if (Widgets.ButtonText(layoutRect, layoutLabel))
            {
                ToggleLayout();
            }

            TooltipHandler.TipRegion(layoutRect, "WorkMonitor.GroupDetailLayoutTip".Translate());

            Rect expandRect = new Rect(layoutRect.xMax + ToolbarGap, y, ExpandAllWidth, 22f);
            if (Widgets.ButtonText(expandRect, "WorkMonitor.ExpandAll".Translate()))
            {
                ExpandOneLevel();
            }

            TooltipHandler.TipRegion(expandRect, "WorkMonitor.ExpandAllLevelTip".Translate());

            Rect collapseRect = new Rect(expandRect.xMax + ToolbarGap, y, ExpandAllWidth, 22f);
            if (Widgets.ButtonText(collapseRect, "WorkMonitor.CollapseAll".Translate()))
            {
                CollapseOneLevel();
            }

            TooltipHandler.TipRegion(collapseRect, "WorkMonitor.ExpandAllLevelTip".Translate());

            y += RowHeight;

            Rect titleRect = new Rect(area.x, y, tableWidth, 22f);
            Widgets.Label(titleRect, "WorkMonitor.Colonists".Translate());
            TooltipHandler.TipRegion(titleRect, "WorkMonitor.ColonistsTip".Translate());

            y += RowHeight;

            DrawBreakdownHeader(new Rect(area.x, y, tableWidth, RowHeight), WorkGiverFirst);
            y += RowHeight;

            int rowIndex = 0;
            if (WorkGiverFirst)
            {
                DrawWorkGiverFirstRows(
                    area,
                    tableWidth,
                    ref y,
                    ref rowIndex,
                    ref colonistClicked,
                    ref selectedColonist,
                    ref workGiverClicked,
                    ref selectedWorkGiver);
            }
            else
            {
                DrawColonistFirstRows(
                    area,
                    tableWidth,
                    ref y,
                    ref rowIndex,
                    ref colonistClicked,
                    ref selectedColonist,
                    ref workGiverClicked,
                    ref selectedWorkGiver);
            }
        }

        private void DrawColonistFirstRows(
            Rect area,
            float tableWidth,
            ref float y,
            ref int rowIndex,
            ref bool colonistClicked,
            ref ColonistWorkStat selectedColonist,
            ref bool workGiverClicked,
            ref WorkGiverDef selectedWorkGiver)
        {
            foreach (ColonistWorkStat colonist in stats.ColonistStats)
            {
                int pawnId = colonist.PawnId;
                bool expanded = expandedColonistIds.Contains(pawnId);

                Rect row = new Rect(area.x, y, tableWidth, RowHeight);
                WorkMonitorUiUtility.DrawRowBackground(row, MonitorRowKind.Colonist, rowIndex);

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
                    out _,
                    out Rect iconsCol,
                    out Rect labelCol,
                    out _,
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

                if (DrawColonistRow(
                    row,
                    colonist,
                    iconsCol,
                    ColonistWorkQuery.FormatColonistSkillGroupInterest(colonist.PawnId, stats.Group),
                    out bool inspectClicked) && inspectClicked && !colonist.IsAbsent)
                {
                    ColonistInspectUtility.OpenPawnProfile(colonist.Pawn);
                }

                y += RowHeight;
                rowIndex++;

                if (expanded)
                {
                    DrawExpandedWorkGivers(area, tableWidth, colonist.PawnId, ref y, ref rowIndex, ref workGiverClicked, ref selectedWorkGiver);
                }
            }

            if (HasMapOnlyBacklog())
            {
                DrawUnassignedBacklogColonistRow(
                    area,
                    tableWidth,
                    ref y,
                    ref rowIndex,
                    ref workGiverClicked,
                    ref selectedWorkGiver);
            }
        }

        private void DrawUnassignedBacklogColonistRow(
            Rect area,
            float tableWidth,
            ref float y,
            ref int rowIndex,
            ref bool workGiverClicked,
            ref WorkGiverDef selectedWorkGiver)
        {
            int pawnId = BulkExpandUtility.UnassignedBacklogPawnId;
            bool expanded = expandedColonistIds.Contains(pawnId);

            Rect row = new Rect(area.x, y, tableWidth, RowHeight);
            WorkMonitorUiUtility.DrawRowBackground(row, MonitorRowKind.Colonist, rowIndex);

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
                out _,
                out _,
                out Rect labelCol,
                out _,
                out Rect kpiJobCol,
                out Rect kpiWorkCol,
                out Rect jobsCol,
                out Rect endlessCol,
                out Rect workCol,
                out Rect walkCol,
                out Rect activeWorkCol);

            Color prev = GUI.color;
            GUI.color = new Color(0.85f, 0.85f, 0.85f);
            Widgets.Label(labelCol, "WorkMonitor.UnassignedBacklog".Translate().Truncate(labelCol.width));
            GUI.color = prev;
            TooltipHandler.TipRegion(row, "WorkMonitor.UnassignedBacklogTip".Translate());

            WorkMonitorUiUtility.LabelRightStatValue(jobsCol, "0");
            WorkMonitorUiUtility.LabelRightStatValue(endlessCol, "0");
            WorkMonitorUiUtility.LabelRightStatValue(workCol, "—");
            WorkMonitorUiUtility.LabelRightStatValue(walkCol, "—");
            WorkMonitorUiUtility.LabelRightStatValue(activeWorkCol, "—");
            WorkMonitorUiUtility.LabelRightStatValue(kpiJobCol, "—");
            WorkMonitorUiUtility.LabelRightStatValue(kpiWorkCol, "—");

            y += RowHeight;
            rowIndex++;

            if (expanded)
            {
                DrawExpandedMapOnlyWorkGivers(area, tableWidth, ref y, ref rowIndex, ref workGiverClicked, ref selectedWorkGiver);
            }
        }

        private void DrawExpandedMapOnlyWorkGivers(
            Rect area,
            float tableWidth,
            ref float y,
            ref int rowIndex,
            ref bool workGiverClicked,
            ref WorkGiverDef selectedWorkGiver)
        {
            bool showHours = WorkMonitorMod.Settings?.showTimeInHours ?? true;

            foreach (WorkGiverDef workGiver in GetMapOnlyWorkGivers())
            {
                Rect columnRow = new Rect(area.x, y, tableWidth, RowHeight);
                if (DrawWorkGiverBreakdownRow(
                    area,
                    columnRow,
                    WorkGiverIndent,
                    WorkGiverLabelUtility.Format(workGiver),
                    0,
                    0,
                    0f,
                    0,
                    0,
                    0,
                    showHours,
                    rowIndex,
                    workGiver,
                    interestPawnId: 0,
                    stats.Group,
                    out WorkGiverDef clickedWg))
                {
                    workGiverClicked = true;
                    selectedWorkGiver = clickedWg;
                }

                y += RowHeight;
                rowIndex++;
            }
        }

        private bool HasMapOnlyBacklog()
        {
            return GetMapOnlyWorkGivers().Count > 0;
        }

        private List<WorkGiverDef> GetMapOnlyWorkGivers()
        {
            return BulkExpandUtility.GetMapOnlyWorkGivers(stats, GetWorkGiverDetail);
        }

        private void DrawWorkGiverFirstRows(
            Rect area,
            float tableWidth,
            ref float y,
            ref int rowIndex,
            ref bool colonistClicked,
            ref ColonistWorkStat selectedColonist,
            ref bool workGiverClicked,
            ref WorkGiverDef selectedWorkGiver)
        {
            bool showHours = WorkMonitorMod.Settings?.showTimeInHours ?? true;

            foreach (WorkGiverDef workGiver in GetActiveWorkGivers())
            {
                WorkGiverDetailStats detail = GetWorkGiverDetail(workGiver);
                bool expanded = expandedWorkGiverDefNames.Contains(workGiver.defName);

                Rect row = new Rect(area.x, y, tableWidth, RowHeight);
                Rect expandRect = new Rect(row.x, row.y, ExpandButtonWidth, row.height);

                if (DrawWorkGiverBreakdownRow(
                    area,
                    row,
                    labelIndentFromArea: null,
                    WorkGiverLabelUtility.Format(workGiver),
                    detail.TotalJobCount,
                    detail.TotalEndlessJobCount,
                    detail.TotalWorkUnits,
                    detail.TotalTicksSpent,
                    detail.TotalTravelTicks,
                    detail.TotalWorkTicks,
                    showHours,
                    rowIndex,
                    workGiver,
                    interestPawnId: 0,
                    stats.Group,
                    out WorkGiverDef clickedWg))
                {
                    workGiverClicked = true;
                    selectedWorkGiver = clickedWg;
                }

                if (Widgets.ButtonText(expandRect, expanded ? "▼" : "▶"))
                {
                    if (expanded)
                    {
                        expandedWorkGiverDefNames.Remove(workGiver.defName);
                    }
                    else
                    {
                        expandedWorkGiverDefNames.Add(workGiver.defName);
                    }
                }

                y += RowHeight;
                rowIndex++;

                if (expanded)
                {
                    DrawExpandedColonists(area, tableWidth, detail, ref y, ref rowIndex, ref colonistClicked, ref selectedColonist);
                }
            }
        }

        private bool AllLevel1Expanded()
        {
            return WorkGiverFirst ? AllWorkGiversExpanded() : AllColonistsExpanded();
        }

        private bool AnyLevel2Expanded()
        {
            return WorkGiverFirst ? expandedWorkGiverDefNames.Count > 0 : expandedColonistIds.Count > 0;
        }

        private void ExpandOneLevel()
        {
            BulkExpandUtility.ExpandOneLevel(AllLevel1Expanded(), ExpandAllLevel1, ExpandAllLevel2);
        }

        private void CollapseOneLevel()
        {
            BulkExpandUtility.CollapseOneLevel(AnyLevel2Expanded(), CollapseAllLevel2, CollapseAllLevel1);
        }

        private void ExpandAllLevel1()
        {
            if (WorkGiverFirst)
            {
                foreach (WorkGiverDef workGiver in GetActiveWorkGivers())
                {
                    expandedWorkGiverDefNames.Add(workGiver.defName);
                }
            }
            else
            {
                foreach (ColonistWorkStat colonist in stats.ColonistStats)
                {
                    expandedColonistIds.Add(colonist.PawnId);
                }

                if (HasMapOnlyBacklog())
                {
                    expandedColonistIds.Add(BulkExpandUtility.UnassignedBacklogPawnId);
                }
            }
        }

        private void ExpandAllLevel2()
        {
            // Detail breakdown has two visual levels but one expansion set; L2 follows L1.
        }

        private void CollapseAllLevel2()
        {
            if (WorkGiverFirst)
            {
                expandedWorkGiverDefNames.Clear();
            }
            else
            {
                expandedColonistIds.Clear();
            }
        }

        private void CollapseAllLevel1()
        {
            CollapseAllLevel2();
        }

        private bool AllColonistsExpanded()
        {
            if (stats.ColonistStats.Count == 0 && !HasMapOnlyBacklog())
            {
                return true;
            }

            foreach (ColonistWorkStat colonist in stats.ColonistStats)
            {
                if (!expandedColonistIds.Contains(colonist.PawnId))
                {
                    return false;
                }
            }

            if (HasMapOnlyBacklog() && !expandedColonistIds.Contains(BulkExpandUtility.UnassignedBacklogPawnId))
            {
                return false;
            }

            return true;
        }

        private bool AllWorkGiversExpanded()
        {
            List<WorkGiverDef> workGivers = GetActiveWorkGivers();
            if (workGivers.Count == 0)
            {
                return true;
            }

            foreach (WorkGiverDef workGiver in workGivers)
            {
                if (!expandedWorkGiverDefNames.Contains(workGiver.defName))
                {
                    return false;
                }
            }

            return true;
        }

        private void DrawExpandedWorkGivers(Rect area, float tableWidth, int pawnId, ref float y, ref int rowIndex, ref bool workGiverClicked, ref WorkGiverDef selectedWorkGiver)
        {
            ColonistGroupWorkDetail detail = GetColonistDetail(pawnId);
            bool showHours = WorkMonitorMod.Settings?.showTimeInHours ?? true;

            foreach (ColonistWorkGiverStat wg in detail.WorkGiverStats)
            {
                Rect columnRow = new Rect(area.x, y, tableWidth, RowHeight);
                if (DrawWorkGiverBreakdownRow(
                    area,
                    columnRow,
                    WorkGiverIndent,
                    wg.Label,
                    wg.JobCount,
                    wg.EndlessJobCount,
                    wg.WorkUnitsSpent,
                    wg.TicksSpent,
                    wg.TravelTicksSpent,
                    wg.WorkTicksSpent,
                    showHours,
                    rowIndex,
                    wg.WorkGiver,
                    pawnId,
                    stats.Group,
                    out WorkGiverDef clickedWg))
                {
                    workGiverClicked = true;
                    selectedWorkGiver = clickedWg;
                }

                y += RowHeight;
                rowIndex++;
            }
        }

        private void DrawExpandedColonists(
            Rect area,
            float tableWidth,
            WorkGiverDetailStats detail,
            ref float y,
            ref int rowIndex,
            ref bool colonistClicked,
            ref ColonistWorkStat selectedColonist)
        {
            foreach (ColonistWorkStat colonist in detail.ColonistStats)
            {
                Rect row = new Rect(area.x + WorkGiverIndent, y, tableWidth - WorkGiverIndent, RowHeight);
                WorkMonitorUiUtility.DrawRowBackground(row, MonitorRowKind.Colonist, rowIndex);

                GetColonistTableColumns(
                    row,
                    out _,
                    out Rect iconsCol,
                    out Rect labelCol,
                    out _,
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

                if (DrawColonistRow(
                    row,
                    colonist,
                    iconsCol,
                    ColonistWorkQuery.FormatColonistWorkGiverInterest(colonist.PawnId, detail.WorkGiver, stats.Group),
                    out bool inspectClicked) && inspectClicked && !colonist.IsAbsent)
                {
                    ColonistInspectUtility.OpenPawnProfile(colonist.Pawn);
                }

                y += RowHeight;
                rowIndex++;
            }
        }

        private bool DrawWorkGiverBreakdownRow(
            Rect area,
            Rect columnRow,
            float? labelIndentFromArea,
            string label,
            int jobCount,
            int endlessCount,
            float workUnits,
            int ticksSpent,
            int travelTicks,
            int workTicks,
            bool showHours,
            int rowIndex,
            WorkGiverDef navigateTarget,
            int interestPawnId,
            WorkGroupSnapshot group,
            out WorkGiverDef clickedWorkGiver)
        {
            clickedWorkGiver = null;
            WorkMonitorUiUtility.DrawRowBackground(columnRow, MonitorRowKind.WorkGiver, rowIndex);

            GetColonistTableColumns(
                columnRow,
                out _,
                out _,
                out Rect labelCol,
                out Rect interestCol,
                out Rect kpiJobCol,
                out Rect kpiWorkCol,
                out Rect jobsCol,
                out Rect endlessCol,
                out Rect workCol,
                out Rect walkCol,
                out Rect activeWorkCol);

            Rect clickRect = new Rect(labelCol.x, columnRow.y, columnRow.xMax - labelCol.x, columnRow.height);
            if (navigateTarget != null && Widgets.ButtonInvisible(clickRect))
            {
                clickedWorkGiver = navigateTarget;
            }

            Text.Font = GameFont.Tiny;
            Color prev = GUI.color;
            GUI.color = new Color(0.8f, 0.8f, 0.8f);
            if (labelIndentFromArea.HasValue)
            {
                float labelWidth = interestCol.x - area.x - labelIndentFromArea.Value - 8f;
                float labelLeft = area.x + labelIndentFromArea.Value;
                WorkGiverLabelUtility.DrawLabelOrText(
                    columnRow,
                    labelLeft,
                    labelWidth,
                    navigateTarget,
                    label,
                    GameFont.Tiny);
            }
            else
            {
                WorkGiverLabelUtility.DrawLabelOrText(
                    columnRow,
                    labelCol.x,
                    labelCol.width,
                    navigateTarget,
                    label,
                    GameFont.Tiny);
            }

            GUI.color = prev;

            if (interestPawnId > 0 && navigateTarget != null)
            {
                WorkMonitorUiUtility.DrawInterestValue(
                    interestCol,
                    ColonistWorkQuery.FormatColonistWorkGiverInterest(interestPawnId, navigateTarget, group));
            }

            float hours = ticksSpent / (float)WorkMonitorSettings.TicksPerHour;
            float jobsPerHour = hours > 0f ? jobCount / hours : 0f;
            float workUnitsPerHour = hours > 0f ? workUnits / hours : 0f;
            WorkMonitorUiUtility.LabelRightWorkGiverStatValue(kpiJobCol, FormatPerHour(jobsPerHour, integer: true));
            WorkMonitorUiUtility.LabelRightWorkGiverStatValue(kpiWorkCol, FormatPerHour(workUnitsPerHour, integer: false));
            WorkMonitorUiUtility.LabelRightWorkGiverStatValue(jobsCol, jobCount.ToString());
            WorkMonitorUiUtility.LabelRightWorkGiverStatValue(endlessCol, endlessCount.ToString());
            WorkMonitorUiUtility.LabelRightWorkGiverStatValue(workCol, WorkMonitorUtility.FormatWorkUnits(workUnits));
            WorkMonitorUiUtility.LabelRightWorkGiverStatValue(walkCol, WorkMonitorUtility.FormatDuration(travelTicks, showHours));
            WorkMonitorUiUtility.LabelRightWorkGiverStatValue(activeWorkCol, WorkMonitorUtility.FormatDuration(workTicks, showHours));
            return clickedWorkGiver != null;
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
            for (int i = 0; i < stats.WorkGiverStats.Count; i++)
            {
                WorkGiverStat wg = stats.WorkGiverStats[i];
                Rect row = new Rect(area.x, y, area.width, RowHeight);
                WorkMonitorUiUtility.DrawRowBackground(row, MonitorRowKind.WorkGiver, rowIndex);

                DrawWorkGiverRow(row, wg, i, out bool clicked);
                if (clicked)
                {
                    workGiverClicked = true;
                    selectedWorkGiver = wg.WorkGiver;
                }
                y += RowHeight;
                rowIndex++;
            }

            Rect totalRow = new Rect(area.x, y, area.width, RowHeight);
            WorkMonitorUiUtility.DrawRowBackground(totalRow, MonitorRowKind.Total, 0);
            DrawWorkGiverTotalRow(totalRow);
            y += RowHeight;
        }

        private static void DrawBreakdownHeader(Rect row, bool workGiverFirst)
        {
            Text.Font = GameFont.Tiny;
            Color prev = GUI.color;
            GUI.color = new Color(0.72f, 0.72f, 0.72f);

            GetColonistTableColumns(
                row,
                out Rect portraitCol,
                out Rect iconsCol,
                out Rect labelCol,
                out Rect interestCol,
                out Rect kpiJobCol,
                out Rect kpiWorkCol,
                out Rect jobsCol,
                out Rect endlessCol,
                out Rect workCol,
                out Rect walkCol,
                out Rect activeWorkCol);

            Widgets.Label(new Rect(row.x, row.y, ExpandButtonWidth, row.height), "");
            Widgets.Label(portraitCol, "");
            Widgets.Label(
                labelCol,
                workGiverFirst
                    ? "WorkMonitor.WorkGiver".Translate()
                    : "WorkMonitor.Colonist".Translate());
            LabelRight(interestCol, "WorkMonitor.Interest".Translate());
            TooltipHandler.TipRegion(interestCol, "WorkMonitor.ColonistInterestTip".Translate());
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

        private static bool DrawColonistRow(Rect row, ColonistWorkStat colonist, Rect iconsCol, string interest, out bool inspectClicked)
        {
            inspectClicked = false;

            Text.Font = GameFont.Small;
            GetColonistTableColumns(
                row,
                out Rect portraitCol,
                out _,
                out Rect labelCol,
                out Rect interestCol,
                out Rect kpiJobCol,
                out Rect kpiWorkCol,
                out Rect jobsCol,
                out Rect endlessCol,
                out Rect workCol,
                out Rect walkCol,
                out Rect activeWorkCol);

            WorkMonitorUiUtility.DrawColonistPortrait(portraitCol, colonist);
            WorkMonitorUiUtility.DrawColonistLabel(labelCol, colonist);
            WorkMonitorUiUtility.DrawInterestValue(interestCol, interest);

            Rect inspectRect = new Rect(iconsCol.x, row.y + (row.height - ColonistIconSize) * 0.5f, ColonistIconSize, ColonistIconSize);
            if (!colonist.IsAbsent)
            {
                if (Widgets.ButtonImage(inspectRect, TexButton.Info))
                {
                    inspectClicked = true;
                    return true;
                }

                TooltipHandler.TipRegion(inspectRect, "WorkMonitor.OpenColonistProfile".Translate());
            }
            else
            {
                Color prevColor = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, 0.25f);
                GUI.DrawTexture(inspectRect, TexButton.Info);
                GUI.color = prevColor;
                TooltipHandler.TipRegion(inspectRect, "WorkMonitor.ColonistAbsentTip".Translate());
            }

            WorkMonitorUiUtility.LabelRightStatValue(kpiJobCol, FormatPerHour(colonist.JobsPerHour, integer: true));
            WorkMonitorUiUtility.LabelRightStatValue(kpiWorkCol, FormatPerHour(colonist.WorkUnitsPerHour, integer: false));
            WorkMonitorUiUtility.LabelRightStatValue(jobsCol, colonist.JobCount.ToString());
            WorkMonitorUiUtility.LabelRightStatValue(endlessCol, colonist.EndlessJobCount.ToString());
            WorkMonitorUiUtility.LabelRightStatValue(workCol, WorkMonitorUtility.FormatWorkUnits(colonist.WorkUnitsSpent));
            WorkMonitorUiUtility.LabelRightStatValue(
                walkCol,
                WorkMonitorUtility.FormatDuration(colonist.TravelTicksSpent, WorkMonitorMod.Settings?.showTimeInHours ?? true));
            WorkMonitorUiUtility.LabelRightStatValue(
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

            Widgets.Label(new Rect(labelCol.x + PieChartPalette.SwatchTotalWidth, labelCol.y, labelCol.width - PieChartPalette.SwatchTotalWidth, labelCol.height), "WorkMonitor.WorkGiver".Translate());
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

        private void DrawWorkGiverRow(Rect row, WorkGiverStat wg, int workGiverIndex, out bool clicked)
        {
            clicked = false;
            Text.Font = GameFont.Small;
            GetMapTableColumns(row, out Rect labelCol, out Rect mapJobsCol, out Rect mapWorkCol, out Rect colonistJobsCol, out Rect endlessCol);

            if (Widgets.ButtonInvisible(row))
            {
                clicked = true;
            }

            bool showSwatch = wg.MapOpenTasks > 0 || wg.MapWorkLeft > 0f;
            PieChartPalette.DrawSwatch(
                row,
                labelCol.x,
                showSwatch ? PieChartPalette.ForWorkGiver(workGiverIndex) : (Color?)null);
            WorkGiverLabelUtility.Draw(
                row,
                labelCol.x + PieChartPalette.SwatchTotalWidth,
                labelCol.width - PieChartPalette.SwatchTotalWidth,
                wg.WorkGiver,
                GameFont.Small);
            string mapJobsText = WorkMonitorUiUtility.FormatMapOpenTasks(wg.MapOpenTasks, wg.MapNewTodayOpenTasks);
            string mapWorkText = WorkMonitorUiUtility.FormatMapWorkLeft(wg.MapWorkLeft, wg.MapNewTodayWorkLeft);
            WorkMonitorUiUtility.LabelRightWorkGiverStatValue(mapJobsCol, mapJobsText);
            TooltipHandler.TipRegion(mapJobsCol, "WorkMonitor.MapJobsNewTodayTip".Translate(mapJobsText));
            WorkMonitorUiUtility.LabelRightWorkGiverStatValue(mapWorkCol, mapWorkText);
            TooltipHandler.TipRegion(mapWorkCol, "WorkMonitor.MapWorkNewTodayTip".Translate(mapWorkText));
            WorkMonitorUiUtility.LabelRightWorkGiverStatValue(colonistJobsCol, wg.JobCount.ToString());
            WorkMonitorUiUtility.LabelRightWorkGiverStatValue(endlessCol, wg.EndlessJobCount.ToString());
        }

        private void DrawWorkGiverTotalRow(Rect row)
        {
            Text.Font = GameFont.Small;
            GetMapTableColumns(row, out Rect labelCol, out Rect mapJobsCol, out Rect mapWorkCol, out Rect colonistJobsCol, out Rect endlessCol);

            Color prev = GUI.color;
            GUI.color = new Color(0.85f, 0.85f, 0.85f);
            Widgets.Label(new Rect(labelCol.x + PieChartPalette.SwatchTotalWidth, labelCol.y, labelCol.width - PieChartPalette.SwatchTotalWidth, labelCol.height), "WorkMonitor.MapTotal".Translate(stats.Group.Label));
            string mapJobsText = WorkMonitorUiUtility.FormatMapOpenTasks(stats.TotalMapOpenTasks, stats.TotalMapNewTodayOpenTasks);
            string mapWorkText = WorkMonitorUiUtility.FormatMapWorkLeft(stats.TotalMapWorkLeft, stats.TotalMapNewTodayWorkLeft);
            WorkMonitorUiUtility.LabelRightStatValue(mapJobsCol, mapJobsText);
            WorkMonitorUiUtility.LabelRightStatValue(mapWorkCol, mapWorkText);
            WorkMonitorUiUtility.LabelRightStatValue(colonistJobsCol, stats.TotalJobCount.ToString());
            WorkMonitorUiUtility.LabelRightStatValue(endlessCol, stats.TotalEndlessJobCount.ToString());
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
            out Rect portraitCol,
            out Rect iconsCol,
            out Rect labelCol,
            out Rect interestCol,
            out Rect kpiJobCol,
            out Rect kpiWorkCol,
            out Rect jobsCol,
            out Rect endlessCol,
            out Rect workCol,
            out Rect walkCol,
            out Rect activeWorkCol)
        {
            WorkMonitorTableColumns.GetGroupDetailColonistTableColumns(
                row,
                out portraitCol,
                out iconsCol,
                out labelCol,
                out interestCol,
                out kpiJobCol,
                out kpiWorkCol,
                out jobsCol,
                out endlessCol,
                out workCol,
                out walkCol,
                out activeWorkCol);
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
