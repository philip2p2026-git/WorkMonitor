using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using WorkMonitor.Groups;

namespace WorkMonitor.UI
{
    public class WorkGroupOverviewPanel
    {
        private const float RowHeight = 30f;
        private const float ExpandAllWidth = 76f;
        private const float LayoutToggleWidth = 96f;
        private const float ToolbarGap = 4f;
        private const float RefreshWidth = 90f;
        private const float RangeWidth = 110f;
        private const float ExpandButtonWidth = WorkMonitorTableColumns.ExpandButtonWidth;
        private const float StatusWidth = WorkMonitorTableColumns.OverviewStatusWidth;
        private const float ColonistIndent = WorkMonitorTableColumns.OverviewColonistIndent;
        private const float WorkGiverIndent = WorkMonitorTableColumns.OverviewWorkGiverIndent;
        private const string EmptyMetric = "—";

        private Vector2 scroll;
        private List<WorkGroupStats> cachedStats = new List<WorkGroupStats>();
        private int lastRefreshTick;
        private MonitorRangeState boundRangeState;
        private readonly HashSet<string> expandedGroupKeys = new HashSet<string>();
        private readonly HashSet<string> expandedLevel2Keys = new HashSet<string>();
        private readonly Dictionary<string, Dictionary<int, ColonistGroupWorkDetail>> colonistDetailCache = new Dictionary<string, Dictionary<int, ColonistGroupWorkDetail>>();
        private readonly Dictionary<string, Dictionary<string, WorkGiverDetailStats>> workGiverDetailCache = new Dictionary<string, Dictionary<string, WorkGiverDetailStats>>();
        private readonly Dictionary<string, List<WorkGiverDef>> activeWorkGiversCache = new Dictionary<string, List<WorkGiverDef>>();
        private List<ColonistOverviewNode> cachedColonistTree = new List<ColonistOverviewNode>();
        private readonly HashSet<string> expandedColonistTopKeys = new HashSet<string>();

        private static OverviewLayoutMode LayoutMode =>
            WorkMonitorMod.Settings?.overviewLayoutMode ?? OverviewLayoutMode.WorkTypeColonistFirst;

        private static bool WorkGiverFirst => LayoutMode == OverviewLayoutMode.WorkTypeWorkGiverFirst;

        private static bool ColonistTopLevel => LayoutMode == OverviewLayoutMode.ColonistTopLevel;

        public void ClearExpandCaches()
        {
            expandedGroupKeys.Clear();
            expandedLevel2Keys.Clear();
            colonistDetailCache.Clear();
            workGiverDetailCache.Clear();
            activeWorkGiversCache.Clear();
            expandedColonistTopKeys.Clear();
            cachedColonistTree.Clear();
        }

        public void RefreshIfNeeded(MonitorRangeState rangeState, bool force = false)
        {
            int refresh = WorkMonitorMod.Settings?.refreshIntervalTicks ?? 60;
            if (!force && Find.TickManager.TicksGame - lastRefreshTick < refresh)
            {
                return;
            }

            if (boundRangeState != null && boundRangeState != rangeState)
            {
                ClearExpandCaches();
            }

            boundRangeState = rangeState;
            cachedStats = WorkGroupStatsAggregator.BuildAll(rangeState.RangeHours);
            cachedColonistTree = ColonistOverviewTreeBuilder.Build(rangeState.RangeHours, cachedStats);
            colonistDetailCache.Clear();
            workGiverDetailCache.Clear();
            activeWorkGiversCache.Clear();
            lastRefreshTick = Find.TickManager.TicksGame;
        }

        public void Draw(
            Rect rect,
            MonitorRangeState rangeState,
            out bool groupClicked,
            out WorkGroupSnapshot selectedGroup,
            out bool colonistClicked,
            out ColonistWorkStat selectedColonist,
            out WorkGroupSnapshot colonistGroup,
            out bool workGiverClicked,
            out WorkGiverDef selectedWorkGiver,
            out WorkGroupSnapshot workGiverGroup)
        {
            groupClicked = false;
            selectedGroup = null;
            colonistClicked = false;
            selectedColonist = null;
            colonistGroup = null;
            workGiverClicked = false;
            selectedWorkGiver = null;
            workGiverGroup = null;

            if (boundRangeState != rangeState)
            {
                RefreshIfNeeded(rangeState, force: true);
            }
            else
            {
                RefreshIfNeeded(rangeState);
            }

            float toolbarRight = rect.xMax;
            Text.Font = GameFont.Tiny;
            if (Widgets.ButtonText(new Rect(toolbarRight - RefreshWidth, rect.y, RefreshWidth, 24f), "WorkMonitor.Refresh".Translate()))
            {
                RefreshIfNeeded(rangeState, force: true);
            }

            toolbarRight -= RefreshWidth + ToolbarGap;
            WorkMonitorDropdownUtility.DrawRangeDropdown(
                new Rect(toolbarRight - RangeWidth, rect.y, RangeWidth, 24f),
                rangeState,
                () => RefreshIfNeeded(rangeState, force: true));
            toolbarRight -= RangeWidth + ToolbarGap;

            float expandCollapseWidth = ExpandAllWidth * 2f + ToolbarGap;
            Rect collapseRect = new Rect(toolbarRight - ExpandAllWidth, rect.y, ExpandAllWidth, 24f);
            if (Widgets.ButtonText(collapseRect, "WorkMonitor.CollapseAll".Translate()))
            {
                CollapseOneLevel();
            }

            TooltipHandler.TipRegion(collapseRect, BulkExpandTooltip());

            Rect expandRect = new Rect(toolbarRight - expandCollapseWidth, rect.y, ExpandAllWidth, 24f);
            if (Widgets.ButtonText(expandRect, "WorkMonitor.ExpandAll".Translate()))
            {
                ExpandOneLevel();
            }

            TooltipHandler.TipRegion(expandRect, BulkExpandTooltip());

            toolbarRight -= expandCollapseWidth + ToolbarGap;

            string layoutLabel = LayoutMode switch
            {
                OverviewLayoutMode.WorkTypeWorkGiverFirst => "WorkMonitor.GroupByWorkGiver".Translate(),
                OverviewLayoutMode.ColonistTopLevel => "WorkMonitor.GroupByColonistTop".Translate(),
                _ => "WorkMonitor.GroupByColonist".Translate()
            };
            Rect layoutRect = new Rect(toolbarRight - LayoutToggleWidth, rect.y, LayoutToggleWidth, 24f);
            if (Widgets.ButtonText(layoutRect, layoutLabel))
            {
                ToggleLayout();
            }

            TooltipHandler.TipRegion(layoutRect, "WorkMonitor.OverviewLayoutTip".Translate());

            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(rect.x, rect.y, layoutRect.x - rect.x - 6f, 22f), "WorkMonitor.OverviewTitle".Translate());

            Rect header = new Rect(rect.x, rect.y + 28f, rect.width, 20f);
            DrawHeader(header);

            Rect listRect = new Rect(rect.x, rect.y + 52f, rect.width, rect.height - 92f);
            float viewHeight = CalculateViewHeight();
            Rect viewRect = new Rect(0f, 0f, listRect.width - 16f, viewHeight);
            Widgets.BeginScrollView(listRect, ref scroll, viewRect);

            float y = 0f;
            int rowIndex = 0;
            if (ColonistTopLevel)
            {
                DrawColonistTopLevelTree(
                    rangeState,
                    viewRect.width,
                    ref y,
                    ref rowIndex,
                    ref groupClicked,
                    ref selectedGroup,
                    ref colonistClicked,
                    ref selectedColonist,
                    ref colonistGroup,
                    ref workGiverClicked,
                    ref selectedWorkGiver,
                    ref workGiverGroup);
            }
            else
            {
                foreach (WorkGroupStats stats in cachedStats)
                {
                    DrawGroupTree(
                        stats,
                        rangeState,
                        viewRect.width,
                        ref y,
                        ref rowIndex,
                        ref groupClicked,
                        ref selectedGroup,
                        ref colonistClicked,
                        ref selectedColonist,
                        ref colonistGroup,
                        ref workGiverClicked,
                        ref selectedWorkGiver,
                        ref workGiverGroup);
                }
            }

            Widgets.EndScrollView();

            DrawFooter(rect, cachedStats);
        }

        private void DrawFooter(Rect rect, List<WorkGroupStats> stats)
        {
            int totalOpenTasks = 0;
            int totalNewTodayTasks = 0;
            float totalMapWork = 0f;
            float totalNewTodayWork = 0f;
            int totalJobs = 0;
            float totalWork = 0f;
            foreach (WorkGroupStats s in stats)
            {
                totalOpenTasks += s.TotalMapOpenTasks;
                totalNewTodayTasks += s.TotalMapNewTodayOpenTasks;
                totalMapWork += s.TotalMapWorkLeft;
                totalNewTodayWork += s.TotalMapNewTodayWorkLeft;
                totalJobs += s.TotalJobCount;
                totalWork += s.TotalWorkUnits;
            }

            Widgets.Label(
                new Rect(rect.x, rect.yMax - 28f, rect.width, 24f),
                "WorkMonitor.FooterSummary".Translate(
                    stats.Count,
                    WorkMonitorUiUtility.FormatMapOpenTasks(totalOpenTasks, totalNewTodayTasks),
                    WorkMonitorUiUtility.FormatMapWorkLeft(totalMapWork, totalNewTodayWork),
                    totalJobs,
                    WorkMonitorUtility.FormatWorkUnits(totalWork)));
        }

        private void DrawGroupTree(
            WorkGroupStats stats,
            MonitorRangeState rangeState,
            float width,
            ref float y,
            ref int rowIndex,
            ref bool groupClicked,
            ref WorkGroupSnapshot selectedGroup,
            ref bool colonistClicked,
            ref ColonistWorkStat selectedColonist,
            ref WorkGroupSnapshot colonistGroup,
            ref bool workGiverClicked,
            ref WorkGiverDef selectedWorkGiver,
            ref WorkGroupSnapshot workGiverGroup)
        {
            string storageKey = stats.Group.Key.StorageKey;
            bool groupExpanded = expandedGroupKeys.Contains(storageKey);

            Rect row = new Rect(0f, y, width, RowHeight);
            WorkMonitorUiUtility.DrawRowBackground(row, MonitorRowKind.WorkType, rowIndex);

            Rect expandRect = new Rect(row.x, row.y, ExpandButtonWidth, row.height);
            if (Widgets.ButtonText(expandRect, groupExpanded ? "▼" : "▶"))
            {
                if (groupExpanded)
                {
                    expandedGroupKeys.Remove(storageKey);
                }
                else
                {
                    expandedGroupKeys.Add(storageKey);
                }
            }

            Rect navRect = new Rect(row.x + ExpandButtonWidth, row.y, row.width - ExpandButtonWidth, row.height);
            if (Widgets.ButtonInvisible(navRect))
            {
                groupClicked = true;
                selectedGroup = stats.Group;
            }

            DrawWorkTypeRow(row, stats);
            y += RowHeight;
            rowIndex++;

            if (!groupExpanded)
            {
                return;
            }

            if (WorkGiverFirst)
            {
                DrawWorkGiverFirstChildren(stats, rangeState, width, storageKey, ref y, ref rowIndex, ref colonistClicked, ref selectedColonist, ref colonistGroup, ref workGiverClicked, ref selectedWorkGiver, ref workGiverGroup);
            }
            else
            {
                DrawColonistFirstChildren(stats, rangeState, width, storageKey, ref y, ref rowIndex, ref colonistClicked, ref selectedColonist, ref colonistGroup, ref workGiverClicked, ref selectedWorkGiver, ref workGiverGroup);
            }
        }

        private void DrawColonistFirstChildren(
            WorkGroupStats stats,
            MonitorRangeState rangeState,
            float width,
            string storageKey,
            ref float y,
            ref int rowIndex,
            ref bool colonistClicked,
            ref ColonistWorkStat selectedColonist,
            ref WorkGroupSnapshot colonistGroup,
            ref bool workGiverClicked,
            ref WorkGiverDef selectedWorkGiver,
            ref WorkGroupSnapshot workGiverGroup)
        {
            foreach (ColonistWorkStat colonist in stats.ColonistStats)
            {
                string level2Key = Level2PawnKey(storageKey, colonist.PawnId);
                bool colonistExpanded = expandedLevel2Keys.Contains(level2Key);

                Rect row = new Rect(ColonistIndent, y, width - ColonistIndent, RowHeight);
                WorkMonitorUiUtility.DrawRowBackground(row, MonitorRowKind.Colonist, rowIndex);

                Rect expandRect = new Rect(row.x, row.y, ExpandButtonWidth, row.height);
                if (Widgets.ButtonText(expandRect, colonistExpanded ? "▼" : "▶"))
                {
                    if (colonistExpanded)
                    {
                        expandedLevel2Keys.Remove(level2Key);
                    }
                    else
                    {
                        expandedLevel2Keys.Add(level2Key);
                    }
                }

                Rect navRect = new Rect(row.x + ExpandButtonWidth, row.y, row.width - ExpandButtonWidth, row.height);
                if (Widgets.ButtonInvisible(navRect))
                {
                    colonistClicked = true;
                    selectedColonist = colonist;
                    colonistGroup = stats.Group;
                }

                DrawColonistL1Row(row, colonist);
                y += RowHeight;
                rowIndex++;

                if (!colonistExpanded)
                {
                    continue;
                }

                ColonistGroupWorkDetail detail = GetColonistDetail(stats.Group, colonist.PawnId, rangeState);
                foreach (ColonistWorkGiverStat wg in detail.WorkGiverStats)
                {
                    Rect wgRow = new Rect(ColonistIndent + WorkGiverIndent, y, width - ColonistIndent - WorkGiverIndent, RowHeight);
                    WorkMonitorUiUtility.DrawRowBackground(wgRow, MonitorRowKind.WorkGiver, rowIndex);

                    if (Widgets.ButtonInvisible(wgRow))
                    {
                        workGiverClicked = true;
                        selectedWorkGiver = wg.WorkGiver;
                        workGiverGroup = stats.Group;
                    }

                    WorkGiverStat mapStat = FindWorkGiverStat(stats, wg.WorkGiver);
                    DrawWorkGiverL2Row(wgRow, wg.Label, mapStat, wg.JobCount, wg.WorkUnitsSpent);
                    y += RowHeight;
                    rowIndex++;
                }
            }

            DrawUnassignedBacklogColonistFirst(
                stats,
                rangeState,
                width,
                storageKey,
                ref y,
                ref rowIndex,
                ref workGiverClicked,
                ref selectedWorkGiver,
                ref workGiverGroup);
        }

        private void DrawUnassignedBacklogColonistFirst(
            WorkGroupStats stats,
            MonitorRangeState rangeState,
            float width,
            string storageKey,
            ref float y,
            ref int rowIndex,
            ref bool workGiverClicked,
            ref WorkGiverDef selectedWorkGiver,
            ref WorkGroupSnapshot workGiverGroup)
        {
            List<WorkGiverDef> mapOnly = GetMapOnlyWorkGivers(stats, rangeState);
            if (mapOnly.Count == 0)
            {
                return;
            }

            string level2Key = Level2PawnKey(storageKey, BulkExpandUtility.UnassignedBacklogPawnId);
            bool expanded = expandedLevel2Keys.Contains(level2Key);

            BulkExpandUtility.SumMapOnlyMetrics(
                stats,
                wg => GetWorkGiverDetail(stats.Group, wg, rangeState),
                out int mapJobs,
                out int mapNewToday,
                out float mapWork,
                out float mapWorkNewToday);

            Rect row = new Rect(ColonistIndent, y, width - ColonistIndent, RowHeight);
            WorkMonitorUiUtility.DrawRowBackground(row, MonitorRowKind.Colonist, rowIndex);

            Rect expandRect = new Rect(row.x, row.y, ExpandButtonWidth, row.height);
            if (Widgets.ButtonText(expandRect, expanded ? "▼" : "▶"))
            {
                if (expanded)
                {
                    expandedLevel2Keys.Remove(level2Key);
                }
                else
                {
                    expandedLevel2Keys.Add(level2Key);
                }
            }

            DrawUnassignedColonistL1Row(row, mapJobs, mapNewToday, mapWork, mapWorkNewToday);
            TooltipHandler.TipRegion(row, "WorkMonitor.UnassignedBacklogTip".Translate());
            y += RowHeight;
            rowIndex++;

            if (!expanded)
            {
                return;
            }

            foreach (WorkGiverDef workGiver in mapOnly)
            {
                WorkGiverStat mapStat = FindWorkGiverStat(stats, workGiver);
                Rect wgRow = new Rect(ColonistIndent + WorkGiverIndent, y, width - ColonistIndent - WorkGiverIndent, RowHeight);
                WorkMonitorUiUtility.DrawRowBackground(wgRow, MonitorRowKind.WorkGiver, rowIndex);

                if (Widgets.ButtonInvisible(wgRow))
                {
                    workGiverClicked = true;
                    selectedWorkGiver = workGiver;
                    workGiverGroup = stats.Group;
                }

                DrawWorkGiverL2Row(wgRow, WorkGiverLabelUtility.Format(workGiver), mapStat, 0, 0f);
                y += RowHeight;
                rowIndex++;
            }
        }

        private void DrawUnassignedColonistL1Row(Rect row, int mapJobs, int mapNewToday, float mapWork, float mapWorkNewToday)
        {
            Text.Font = GameFont.Small;
            float labelLeft = WorkMonitorTableColumns.OverviewLabelLeft(row.x, hasExpand: true, hasStatus: false);
            float labelWidth = WorkMonitorTableColumns.OverviewLabelWidth(row, labelLeft, hasInterest: false);
            Color prev = GUI.color;
            GUI.color = new Color(0.85f, 0.85f, 0.85f);
            Widgets.Label(new Rect(labelLeft, row.y, labelWidth, row.height), "WorkMonitor.UnassignedBacklog".Translate().Truncate(labelWidth));
            GUI.color = prev;

            WorkMonitorTableColumns.GetOverviewMetricColumns(row, out Rect existJobCol, out Rect existWorkCol, out Rect jobProcessedCol, out Rect workProcessedCol);
            WorkMonitorUiUtility.LabelRightStatValue(existJobCol, WorkMonitorUiUtility.FormatMapOpenTasks(mapJobs, mapNewToday));
            WorkMonitorUiUtility.LabelRightStatValue(existWorkCol, WorkMonitorUiUtility.FormatMapWorkLeft(mapWork, mapWorkNewToday));
            WorkMonitorUiUtility.LabelRightStatValue(jobProcessedCol, "0");
            WorkMonitorUiUtility.LabelRightStatValue(workProcessedCol, EmptyMetric);
        }

        private List<WorkGiverDef> GetMapOnlyWorkGivers(WorkGroupStats stats, MonitorRangeState rangeState)
        {
            return BulkExpandUtility.GetMapOnlyWorkGivers(stats, wg => GetWorkGiverDetail(stats.Group, wg, rangeState));
        }

        private void DrawColonistTopLevelTree(
            MonitorRangeState rangeState,
            float width,
            ref float y,
            ref int rowIndex,
            ref bool groupClicked,
            ref WorkGroupSnapshot selectedGroup,
            ref bool colonistClicked,
            ref ColonistWorkStat selectedColonist,
            ref WorkGroupSnapshot colonistGroup,
            ref bool workGiverClicked,
            ref WorkGiverDef selectedWorkGiver,
            ref WorkGroupSnapshot workGiverGroup)
        {
            foreach (ColonistOverviewNode node in cachedColonistTree)
            {
                string colonistKey = ColonistTopColonistKey(node.PawnId);
                bool colonistExpanded = expandedColonistTopKeys.Contains(colonistKey);

                Rect row = new Rect(0f, y, width, RowHeight);
                WorkMonitorUiUtility.DrawRowBackground(row, MonitorRowKind.Colonist, rowIndex);

                Rect expandRect = new Rect(row.x, row.y, ExpandButtonWidth, row.height);
                if (Widgets.ButtonText(expandRect, colonistExpanded ? "▼" : "▶"))
                {
                    if (colonistExpanded)
                    {
                        expandedColonistTopKeys.Remove(colonistKey);
                    }
                    else
                    {
                        expandedColonistTopKeys.Add(colonistKey);
                    }
                }

                if (!node.IsUnassigned)
                {
                    Rect navRect = new Rect(row.x + ExpandButtonWidth, row.y, row.width - ExpandButtonWidth, row.height);
                    if (Widgets.ButtonInvisible(navRect))
                    {
                        colonistClicked = true;
                        selectedColonist = node.Summary;
                        colonistGroup = null;
                    }

                    DrawColonistL1Row(row, node.Summary);
                }
                else
                {
                    DrawUnassignedColonistTopL0Row(row, node);
                    TooltipHandler.TipRegion(row, "WorkMonitor.UnassignedBacklogTip".Translate());
                }

                y += RowHeight;
                rowIndex++;

                if (!colonistExpanded)
                {
                    continue;
                }

                foreach (ColonistOverviewGroupNode groupNode in node.Groups)
                {
                    string groupKey = ColonistTopGroupKey(node.PawnId, groupNode.Group.Key.StorageKey);
                    bool groupExpanded = expandedColonistTopKeys.Contains(groupKey);
                    WorkGroupStats groupStats = groupNode.GroupStats;

                    Rect groupRow = new Rect(ColonistIndent, y, width - ColonistIndent, RowHeight);
                    WorkMonitorUiUtility.DrawRowBackground(groupRow, MonitorRowKind.WorkType, rowIndex);

                    Rect groupExpandRect = new Rect(groupRow.x, groupRow.y, ExpandButtonWidth, groupRow.height);
                    if (Widgets.ButtonText(groupExpandRect, groupExpanded ? "▼" : "▶"))
                    {
                        if (groupExpanded)
                        {
                            expandedColonistTopKeys.Remove(groupKey);
                        }
                        else
                        {
                            expandedColonistTopKeys.Add(groupKey);
                        }
                    }

                    Rect groupNavRect = new Rect(groupRow.x + ExpandButtonWidth, groupRow.y, groupRow.width - ExpandButtonWidth, groupRow.height);
                    if (Widgets.ButtonInvisible(groupNavRect))
                    {
                        groupClicked = true;
                        selectedGroup = groupNode.Group;
                    }

                    DrawColonistTopGroupRow(groupRow, groupNode, node.IsUnassigned);
                    y += RowHeight;
                    rowIndex++;

                    if (!groupExpanded)
                    {
                        continue;
                    }

                    if (node.IsUnassigned)
                    {
                        foreach (ColonistOverviewMapOnlyEntry entry in groupNode.MapOnlyWorkGivers)
                        {
                            Rect wgRow = new Rect(ColonistIndent + WorkGiverIndent, y, width - ColonistIndent - WorkGiverIndent, RowHeight);
                            WorkMonitorUiUtility.DrawRowBackground(wgRow, MonitorRowKind.WorkGiver, rowIndex);

                            if (Widgets.ButtonInvisible(wgRow))
                            {
                                workGiverClicked = true;
                                selectedWorkGiver = entry.WorkGiver;
                                workGiverGroup = groupNode.Group;
                            }

                            DrawWorkGiverL2Row(wgRow, entry.Label, entry.MapStat, 0, 0f);
                            y += RowHeight;
                            rowIndex++;
                        }
                    }
                    else
                    {
                        foreach (ColonistWorkGiverStat wg in groupNode.ColonistWorkGivers)
                        {
                            WorkGiverStat mapStat = FindWorkGiverStat(groupStats, wg.WorkGiver);
                            Rect wgRow = new Rect(ColonistIndent + WorkGiverIndent, y, width - ColonistIndent - WorkGiverIndent, RowHeight);
                            WorkMonitorUiUtility.DrawRowBackground(wgRow, MonitorRowKind.WorkGiver, rowIndex);

                            if (Widgets.ButtonInvisible(wgRow))
                            {
                                workGiverClicked = true;
                                selectedWorkGiver = wg.WorkGiver;
                                workGiverGroup = groupNode.Group;
                            }

                            DrawWorkGiverL2Row(wgRow, wg.Label, mapStat, wg.JobCount, wg.WorkUnitsSpent);
                            y += RowHeight;
                            rowIndex++;
                        }
                    }
                }
            }
        }

        private static void DrawUnassignedColonistTopL0Row(Rect row, ColonistOverviewNode node)
        {
            Text.Font = GameFont.Small;
            float labelLeft = WorkMonitorTableColumns.OverviewLabelLeft(row.x, hasExpand: true, hasStatus: false);
            float labelWidth = WorkMonitorTableColumns.OverviewLabelWidth(row, labelLeft, hasInterest: false);
            Color prev = GUI.color;
            GUI.color = new Color(0.85f, 0.85f, 0.85f);
            Widgets.Label(new Rect(labelLeft, row.y, labelWidth, row.height), node.Summary.Label.Truncate(labelWidth));
            GUI.color = prev;

            int mapJobs = 0;
            int mapNewToday = 0;
            float mapWork = 0f;
            float mapWorkNewToday = 0f;
            foreach (ColonistOverviewGroupNode groupNode in node.Groups)
            {
                foreach (ColonistOverviewMapOnlyEntry entry in groupNode.MapOnlyWorkGivers)
                {
                    if (entry.MapStat == null)
                    {
                        continue;
                    }

                    mapJobs += entry.MapStat.MapOpenTasks;
                    mapNewToday += entry.MapStat.MapNewTodayOpenTasks;
                    mapWork += entry.MapStat.MapWorkLeft;
                    mapWorkNewToday += entry.MapStat.MapNewTodayWorkLeft;
                }
            }

            DrawOverviewMetrics(row, mapJobs, mapNewToday, mapWork, mapWorkNewToday, 0, 0f);
        }

        private static void DrawColonistTopGroupRow(Rect row, ColonistOverviewGroupNode groupNode, bool isUnassignedParent)
        {
            WorkGroupStats groupStats = groupNode.GroupStats;
            Text.Font = GameFont.Small;
            float labelLeft = WorkMonitorTableColumns.OverviewLabelLeft(row.x, hasExpand: true, hasStatus: true);
            float labelWidth = WorkMonitorTableColumns.OverviewLabelWidth(row, labelLeft, hasInterest: false);

            Color dot = WorkMonitorUiUtility.StatusColor(groupStats.Status);
            float dotSize = 10f;
            float statusX = row.x + ExpandButtonWidth;
            Widgets.DrawBoxSolid(
                new Rect(statusX + (StatusWidth - dotSize) * 0.5f, row.y + (row.height - dotSize) * 0.5f, dotSize, dotSize),
                dot);

            Widgets.Label(new Rect(labelLeft, row.y, labelWidth, row.height), groupNode.Group.Label.Truncate(labelWidth));

            int jobCount = 0;
            float workUnits = 0f;
            if (!isUnassignedParent)
            {
                foreach (ColonistWorkGiverStat wg in groupNode.ColonistWorkGivers)
                {
                    jobCount += wg.JobCount;
                    workUnits += wg.WorkUnitsSpent;
                }
            }

            DrawOverviewMetrics(
                row,
                groupStats.TotalMapOpenTasks,
                groupStats.TotalMapNewTodayOpenTasks,
                groupStats.TotalMapWorkLeft,
                groupStats.TotalMapNewTodayWorkLeft,
                jobCount,
                workUnits);
        }

        private static string ColonistTopColonistKey(int pawnId)
        {
            return "ct:colonist:" + pawnId;
        }

        private static string ColonistTopGroupKey(int pawnId, string storageKey)
        {
            return "ct:colonist:" + pawnId + ":group:" + storageKey;
        }

        private void DrawWorkGiverFirstChildren(
            WorkGroupStats stats,
            MonitorRangeState rangeState,
            float width,
            string storageKey,
            ref float y,
            ref int rowIndex,
            ref bool colonistClicked,
            ref ColonistWorkStat selectedColonist,
            ref WorkGroupSnapshot colonistGroup,
            ref bool workGiverClicked,
            ref WorkGiverDef selectedWorkGiver,
            ref WorkGroupSnapshot workGiverGroup)
        {
            foreach (WorkGiverDef workGiver in GetActiveWorkGivers(stats, rangeState))
            {
                string level2Key = Level2WorkGiverKey(storageKey, workGiver.defName);
                bool workGiverExpanded = expandedLevel2Keys.Contains(level2Key);
                WorkGiverDetailStats detail = GetWorkGiverDetail(stats.Group, workGiver, rangeState);
                WorkGiverStat mapStat = FindWorkGiverStat(stats, workGiver);

                Rect row = new Rect(ColonistIndent, y, width - ColonistIndent, RowHeight);
                WorkMonitorUiUtility.DrawRowBackground(row, MonitorRowKind.WorkGiver, rowIndex);

                Rect expandRect = new Rect(row.x, row.y, ExpandButtonWidth, row.height);
                if (Widgets.ButtonText(expandRect, workGiverExpanded ? "▼" : "▶"))
                {
                    if (workGiverExpanded)
                    {
                        expandedLevel2Keys.Remove(level2Key);
                    }
                    else
                    {
                        expandedLevel2Keys.Add(level2Key);
                    }
                }

                Rect navRect = new Rect(row.x + ExpandButtonWidth, row.y, row.width - ExpandButtonWidth, row.height);
                if (Widgets.ButtonInvisible(navRect))
                {
                    workGiverClicked = true;
                    selectedWorkGiver = workGiver;
                    workGiverGroup = stats.Group;
                }

                DrawWorkGiverL1Row(row, WorkGiverLabelUtility.Format(workGiver), mapStat, detail.TotalJobCount, detail.TotalWorkUnits);
                y += RowHeight;
                rowIndex++;

                if (!workGiverExpanded)
                {
                    continue;
                }

                foreach (ColonistWorkStat colonist in detail.ColonistStats)
                {
                    Rect colonistRow = new Rect(ColonistIndent + WorkGiverIndent, y, width - ColonistIndent - WorkGiverIndent, RowHeight);
                    WorkMonitorUiUtility.DrawRowBackground(colonistRow, MonitorRowKind.Colonist, rowIndex);

                    if (Widgets.ButtonInvisible(colonistRow))
                    {
                        colonistClicked = true;
                        selectedColonist = colonist;
                        colonistGroup = stats.Group;
                    }

                    DrawColonistL2Row(colonistRow, colonist);
                    y += RowHeight;
                    rowIndex++;
                }
            }
        }

        private static void DrawWorkTypeRow(Rect row, WorkGroupStats stats)
        {
            Text.Font = GameFont.Small;
            float labelLeft = WorkMonitorTableColumns.OverviewLabelLeft(row.x, hasExpand: true, hasStatus: true);
            float labelWidth = WorkMonitorTableColumns.OverviewLabelWidth(row, labelLeft, hasInterest: true);

            Color dot = WorkMonitorUiUtility.StatusColor(stats.Status);
            float dotSize = 10f;
            float statusX = row.x + ExpandButtonWidth;
            Widgets.DrawBoxSolid(
                new Rect(statusX + (StatusWidth - dotSize) * 0.5f, row.y + (row.height - dotSize) * 0.5f, dotSize, dotSize),
                dot);

            Widgets.Label(new Rect(labelLeft, row.y, labelWidth, row.height), stats.Group.Label.Truncate(labelWidth));

            WorkMonitorTableColumns.GetOverviewInterestColumn(row, out Rect interestCol);
            WorkMonitorUiUtility.LabelRightStatValue(interestCol, WorkMonitorUiUtility.FormatInterestRatio(stats));

            DrawOverviewMetrics(row, stats.TotalMapOpenTasks, stats.TotalMapNewTodayOpenTasks, stats.TotalMapWorkLeft, stats.TotalMapNewTodayWorkLeft, stats.TotalJobCount, stats.TotalWorkUnits);
        }

        private void DrawColonistL1Row(Rect row, ColonistWorkStat colonist)
        {
            Text.Font = GameFont.Small;
            float labelLeft = WorkMonitorTableColumns.OverviewLabelLeft(row.x, hasExpand: true, hasStatus: false);
            float portraitWidth = WorkMonitorUiUtility.ColonistPortraitSize + 4f;
            Rect portraitRect = new Rect(labelLeft, row.y, portraitWidth, row.height);
            WorkMonitorUiUtility.DrawColonistPortrait(portraitRect, colonist);

            float nameLeft = labelLeft + portraitWidth;
            float nameWidth = WorkMonitorTableColumns.OverviewLabelWidth(row, nameLeft, hasInterest: false);
            WorkMonitorUiUtility.DrawColonistLabel(new Rect(nameLeft, row.y, nameWidth, row.height), colonist);

            DrawOverviewColonistMetrics(row, colonist.JobCount, colonist.WorkUnitsSpent);
        }

        private void DrawColonistL2Row(Rect row, ColonistWorkStat colonist)
        {
            Text.Font = GameFont.Small;
            float labelLeft = row.x + 4f;
            float portraitWidth = WorkMonitorUiUtility.ColonistPortraitSize + 4f;
            Rect portraitRect = new Rect(labelLeft, row.y, portraitWidth, row.height);
            WorkMonitorUiUtility.DrawColonistPortrait(portraitRect, colonist);

            float nameLeft = labelLeft + portraitWidth;
            float nameWidth = WorkMonitorTableColumns.OverviewLabelWidth(row, nameLeft, hasInterest: false);
            WorkMonitorUiUtility.DrawColonistLabel(new Rect(nameLeft, row.y, nameWidth, row.height), colonist);

            DrawOverviewColonistMetrics(row, colonist.JobCount, colonist.WorkUnitsSpent);
        }

        private static void DrawWorkGiverL1Row(Rect row, string label, WorkGiverStat mapStat, int jobCount, float workUnits)
        {
            Text.Font = GameFont.Small;
            float labelLeft = WorkMonitorTableColumns.OverviewLabelLeft(row.x, hasExpand: true, hasStatus: false);
            float labelWidth = WorkMonitorTableColumns.OverviewLabelWidth(row, labelLeft, hasInterest: false);
            Widgets.Label(new Rect(labelLeft, row.y, labelWidth, row.height), label.Truncate(labelWidth));

            int mapJobs = mapStat?.MapOpenTasks ?? 0;
            int mapNewToday = mapStat?.MapNewTodayOpenTasks ?? 0;
            float mapWork = mapStat?.MapWorkLeft ?? 0f;
            float mapWorkNewToday = mapStat?.MapNewTodayWorkLeft ?? 0f;
            DrawOverviewMetrics(row, mapJobs, mapNewToday, mapWork, mapWorkNewToday, jobCount, workUnits);
        }

        private static void DrawWorkGiverL2Row(Rect row, string label, WorkGiverStat mapStat, int jobCount, float workUnits)
        {
            Text.Font = GameFont.Tiny;
            Color prev = GUI.color;
            GUI.color = new Color(0.8f, 0.8f, 0.8f);
            float labelLeft = row.x + 4f;
            float labelWidth = WorkMonitorTableColumns.OverviewLabelWidth(row, labelLeft, hasInterest: false);
            Widgets.Label(new Rect(labelLeft, row.y, labelWidth, row.height), label.Truncate(labelWidth));
            GUI.color = prev;

            int mapJobs = mapStat?.MapOpenTasks ?? 0;
            int mapNewToday = mapStat?.MapNewTodayOpenTasks ?? 0;
            float mapWork = mapStat?.MapWorkLeft ?? 0f;
            float mapWorkNewToday = mapStat?.MapNewTodayWorkLeft ?? 0f;
            DrawOverviewMetrics(row, mapJobs, mapNewToday, mapWork, mapWorkNewToday, jobCount, workUnits);
        }

        private static void DrawOverviewColonistMetrics(Rect row, int jobCount, float workUnits)
        {
            WorkMonitorTableColumns.GetOverviewMetricColumns(row, out Rect existJobCol, out Rect existWorkCol, out Rect jobProcessedCol, out Rect workProcessedCol);
            WorkMonitorUiUtility.LabelRightStatValue(existJobCol, EmptyMetric);
            WorkMonitorUiUtility.LabelRightStatValue(existWorkCol, EmptyMetric);
            WorkMonitorUiUtility.LabelRightStatValue(jobProcessedCol, jobCount.ToString());
            WorkMonitorUiUtility.LabelRightStatValue(workProcessedCol, WorkMonitorUtility.FormatWorkUnits(workUnits));
        }

        private static void DrawOverviewMetrics(Rect row, int mapJobs, int mapNewToday, float mapWork, float mapWorkNewToday, int jobCount, float workUnits)
        {
            WorkMonitorTableColumns.GetOverviewMetricColumns(row, out Rect existJobCol, out Rect existWorkCol, out Rect jobProcessedCol, out Rect workProcessedCol);
            string jobsText = WorkMonitorUiUtility.FormatMapOpenTasks(mapJobs, mapNewToday);
            string workText = WorkMonitorUiUtility.FormatMapWorkLeft(mapWork, mapWorkNewToday);
            WorkMonitorUiUtility.LabelRightStatValue(existJobCol, jobsText);
            TooltipHandler.TipRegion(existJobCol, "WorkMonitor.MapJobsNewTodayTip".Translate(jobsText));
            WorkMonitorUiUtility.LabelRightStatValue(existWorkCol, workText);
            TooltipHandler.TipRegion(existWorkCol, "WorkMonitor.MapWorkNewTodayTip".Translate(workText));
            WorkMonitorUiUtility.LabelRightStatValue(jobProcessedCol, jobCount.ToString());
            WorkMonitorUiUtility.LabelRightStatValue(workProcessedCol, WorkMonitorUtility.FormatWorkUnits(workUnits));
        }

        private static void DrawHeader(Rect rect)
        {
            Text.Font = GameFont.Tiny;
            Color prev = GUI.color;
            GUI.color = new Color(0.72f, 0.72f, 0.72f);

            float labelLeft = WorkMonitorTableColumns.OverviewLabelLeft(rect.x, hasExpand: true, hasStatus: true);
            float labelWidth = WorkMonitorTableColumns.OverviewLabelWidth(rect, labelLeft, hasInterest: true);
            Widgets.Label(new Rect(rect.x + ExpandButtonWidth, rect.y, StatusWidth, rect.height), "WorkMonitor.Status".Translate());
            Widgets.Label(new Rect(labelLeft, rect.y, labelWidth, rect.height), "WorkMonitor.Group".Translate());
            WorkMonitorTableColumns.DrawOverviewMetricHeader(rect, LabelRight);

            GUI.color = prev;
        }

        private float CalculateViewHeight()
        {
            if (ColonistTopLevel)
            {
                return CalculateColonistTopViewHeight();
            }

            float height = cachedStats.Count * RowHeight;

            foreach (WorkGroupStats stats in cachedStats)
            {
                string storageKey = stats.Group.Key.StorageKey;
                if (!expandedGroupKeys.Contains(storageKey))
                {
                    continue;
                }

                if (WorkGiverFirst)
                {
                    List<WorkGiverDef> workGivers = GetActiveWorkGivers(stats, boundRangeState);
                    height += workGivers.Count * RowHeight;
                    foreach (WorkGiverDef workGiver in workGivers)
                    {
                        if (expandedLevel2Keys.Contains(Level2WorkGiverKey(storageKey, workGiver.defName)))
                        {
                            WorkGiverDetailStats detail = GetWorkGiverDetail(stats.Group, workGiver, boundRangeState);
                            height += detail.ColonistStats.Count * RowHeight;
                        }
                    }
                }
                else
                {
                    height += stats.ColonistStats.Count * RowHeight;
                    if (BulkExpandUtility.HasMapOnlyBacklog(stats, wg => GetWorkGiverDetail(stats.Group, wg, boundRangeState)))
                    {
                        height += RowHeight;
                    }

                    foreach (ColonistWorkStat colonist in stats.ColonistStats)
                    {
                        if (expandedLevel2Keys.Contains(Level2PawnKey(storageKey, colonist.PawnId)))
                        {
                            ColonistGroupWorkDetail detail = GetColonistDetail(stats.Group, colonist.PawnId, boundRangeState);
                            height += detail.WorkGiverStats.Count * RowHeight;
                        }
                    }

                    if (expandedLevel2Keys.Contains(Level2PawnKey(storageKey, BulkExpandUtility.UnassignedBacklogPawnId)))
                    {
                        height += GetMapOnlyWorkGivers(stats, boundRangeState).Count * RowHeight;
                    }
                }
            }

            return height;
        }

        private float CalculateColonistTopViewHeight()
        {
            float height = cachedColonistTree.Count * RowHeight;

            foreach (ColonistOverviewNode node in cachedColonistTree)
            {
                if (!expandedColonistTopKeys.Contains(ColonistTopColonistKey(node.PawnId)))
                {
                    continue;
                }

                height += node.Groups.Count * RowHeight;
                foreach (ColonistOverviewGroupNode groupNode in node.Groups)
                {
                    if (!expandedColonistTopKeys.Contains(ColonistTopGroupKey(node.PawnId, groupNode.Group.Key.StorageKey)))
                    {
                        continue;
                    }

                    if (node.IsUnassigned)
                    {
                        height += groupNode.MapOnlyWorkGivers.Count * RowHeight;
                    }
                    else
                    {
                        height += groupNode.ColonistWorkGivers.Count * RowHeight;
                    }
                }
            }

            return height;
        }

        private ColonistGroupWorkDetail GetColonistDetail(WorkGroupSnapshot group, int pawnId, MonitorRangeState rangeState)
        {
            string storageKey = group.Key.StorageKey;
            if (!colonistDetailCache.TryGetValue(storageKey, out Dictionary<int, ColonistGroupWorkDetail> perGroup))
            {
                perGroup = new Dictionary<int, ColonistGroupWorkDetail>();
                colonistDetailCache[storageKey] = perGroup;
            }

            if (!perGroup.TryGetValue(pawnId, out ColonistGroupWorkDetail detail))
            {
                detail = ColonistStatsAggregator.BuildGroupDetail(pawnId, group, rangeState?.RangeHours ?? 24);
                perGroup[pawnId] = detail;
            }

            return detail;
        }

        private WorkGiverDetailStats GetWorkGiverDetail(WorkGroupSnapshot group, WorkGiverDef workGiver, MonitorRangeState rangeState)
        {
            string storageKey = group.Key.StorageKey;
            if (!workGiverDetailCache.TryGetValue(storageKey, out Dictionary<string, WorkGiverDetailStats> perGroup))
            {
                perGroup = new Dictionary<string, WorkGiverDetailStats>();
                workGiverDetailCache[storageKey] = perGroup;
            }

            if (!perGroup.TryGetValue(workGiver.defName, out WorkGiverDetailStats detail))
            {
                detail = WorkGiverStatsAggregator.Build(group, workGiver, rangeState?.RangeHours ?? 24);
                perGroup[workGiver.defName] = detail;
            }

            return detail;
        }

        private List<WorkGiverDef> GetActiveWorkGivers(WorkGroupStats stats, MonitorRangeState rangeState)
        {
            string storageKey = stats.Group.Key.StorageKey;
            if (activeWorkGiversCache.TryGetValue(storageKey, out List<WorkGiverDef> cached))
            {
                return cached;
            }

            var ranked = new List<(WorkGiverDef workGiver, int ticks, float mapWork, int mapOpen)>();
            foreach (WorkGiverDef workGiver in stats.Group.WorkGivers)
            {
                WorkGiverDetailStats detail = GetWorkGiverDetail(stats.Group, workGiver, rangeState);
                WorkGiverStat mapStat = FindWorkGiverStat(stats, workGiver);
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

            cached = ranked
                .OrderByDescending(entry => entry.ticks)
                .ThenByDescending(entry => entry.mapWork)
                .ThenByDescending(entry => entry.mapOpen)
                .Select(entry => entry.workGiver)
                .ToList();
            activeWorkGiversCache[storageKey] = cached;
            return cached;
        }

        private static WorkGiverStat FindWorkGiverStat(WorkGroupStats stats, WorkGiverDef workGiver)
        {
            if (workGiver == null)
            {
                return null;
            }

            return stats.WorkGiverStats.Find(wg => wg.WorkGiver == workGiver);
        }

        private void ToggleLayout()
        {
            WorkMonitorSettings settings = WorkMonitorMod.Settings;
            if (settings == null)
            {
                return;
            }

            settings.overviewLayoutMode = LayoutMode switch
            {
                OverviewLayoutMode.WorkTypeColonistFirst => OverviewLayoutMode.WorkTypeWorkGiverFirst,
                OverviewLayoutMode.WorkTypeWorkGiverFirst => OverviewLayoutMode.ColonistTopLevel,
                _ => OverviewLayoutMode.WorkTypeColonistFirst
            };
            ClearExpandCaches();
        }

        private void ExpandOneLevel()
        {
            if (ColonistTopLevel)
            {
                BulkExpandUtility.ExpandOneLevel(AllColonistTopL0Expanded(), ExpandAllColonistTopL0, ExpandAllColonistTopL1);
            }
            else
            {
                BulkExpandUtility.ExpandOneLevel(AllLevel1Expanded(), ExpandAllLevel1, ExpandAllLevel2);
            }
        }

        private void CollapseOneLevel()
        {
            if (ColonistTopLevel)
            {
                BulkExpandUtility.CollapseOneLevel(AnyColonistTopL1Expanded(), CollapseAllColonistTopL1, CollapseAllColonistTopL0);
            }
            else
            {
                BulkExpandUtility.CollapseOneLevel(AnyLevel2Expanded(), CollapseAllLevel2, CollapseAllLevel1);
            }
        }

        private bool AllLevel1Expanded()
        {
            if (cachedStats.Count == 0)
            {
                return true;
            }

            foreach (WorkGroupStats stats in cachedStats)
            {
                if (!expandedGroupKeys.Contains(stats.Group.Key.StorageKey))
                {
                    return false;
                }
            }

            return true;
        }

        private bool AnyLevel2Expanded()
        {
            return expandedLevel2Keys.Count > 0;
        }

        private void ExpandAllLevel1()
        {
            foreach (WorkGroupStats stats in cachedStats)
            {
                expandedGroupKeys.Add(stats.Group.Key.StorageKey);
            }
        }

        private void ExpandAllLevel2()
        {
            int rangeHours = boundRangeState?.RangeHours ?? 24;
            foreach (WorkGroupStats stats in cachedStats)
            {
                string storageKey = stats.Group.Key.StorageKey;
                if (!expandedGroupKeys.Contains(storageKey))
                {
                    continue;
                }

                if (WorkGiverFirst)
                {
                    foreach (WorkGiverDef workGiver in GetActiveWorkGivers(stats, boundRangeState))
                    {
                        expandedLevel2Keys.Add(Level2WorkGiverKey(storageKey, workGiver.defName));
                    }
                }
                else
                {
                    foreach (ColonistWorkStat colonist in stats.ColonistStats)
                    {
                        expandedLevel2Keys.Add(Level2PawnKey(storageKey, colonist.PawnId));
                    }

                    if (BulkExpandUtility.HasMapOnlyBacklog(stats, wg => GetWorkGiverDetail(stats.Group, wg, boundRangeState)))
                    {
                        expandedLevel2Keys.Add(Level2PawnKey(storageKey, BulkExpandUtility.UnassignedBacklogPawnId));
                    }
                }
            }
        }

        private bool AllColonistTopL0Expanded()
        {
            if (cachedColonistTree.Count == 0)
            {
                return true;
            }

            foreach (ColonistOverviewNode node in cachedColonistTree)
            {
                if (!expandedColonistTopKeys.Contains(ColonistTopColonistKey(node.PawnId)))
                {
                    return false;
                }
            }

            return true;
        }

        private bool AnyColonistTopL1Expanded()
        {
            foreach (ColonistOverviewNode node in cachedColonistTree)
            {
                if (!expandedColonistTopKeys.Contains(ColonistTopColonistKey(node.PawnId)))
                {
                    continue;
                }

                foreach (ColonistOverviewGroupNode groupNode in node.Groups)
                {
                    if (expandedColonistTopKeys.Contains(ColonistTopGroupKey(node.PawnId, groupNode.Group.Key.StorageKey)))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private void ExpandAllColonistTopL0()
        {
            foreach (ColonistOverviewNode node in cachedColonistTree)
            {
                expandedColonistTopKeys.Add(ColonistTopColonistKey(node.PawnId));
            }
        }

        private void ExpandAllColonistTopL1()
        {
            foreach (ColonistOverviewNode node in cachedColonistTree)
            {
                if (!expandedColonistTopKeys.Contains(ColonistTopColonistKey(node.PawnId)))
                {
                    continue;
                }

                foreach (ColonistOverviewGroupNode groupNode in node.Groups)
                {
                    expandedColonistTopKeys.Add(ColonistTopGroupKey(node.PawnId, groupNode.Group.Key.StorageKey));
                }
            }
        }

        private void CollapseAllColonistTopL1()
        {
            var groupKeys = new List<string>();
            foreach (string key in expandedColonistTopKeys)
            {
                if (key.Contains(":group:"))
                {
                    groupKeys.Add(key);
                }
            }

            foreach (string key in groupKeys)
            {
                expandedColonistTopKeys.Remove(key);
            }
        }

        private void CollapseAllColonistTopL0()
        {
            expandedColonistTopKeys.Clear();
        }

        private void CollapseAllLevel2()
        {
            expandedLevel2Keys.Clear();
        }

        private void CollapseAllLevel1()
        {
            expandedGroupKeys.Clear();
            expandedLevel2Keys.Clear();
        }

        private static string Level2PawnKey(string storageKey, int pawnId)
        {
            return storageKey + ":pawn:" + pawnId;
        }

        private static string Level2WorkGiverKey(string storageKey, string defName)
        {
            return storageKey + ":wg:" + defName;
        }

        private static string BulkExpandTooltip()
        {
            return "WorkMonitor.ExpandAllLevelTip".Translate();
        }

        private static void LabelRight(Rect rect, string text)
        {
            Text.Anchor = TextAnchor.MiddleRight;
            Widgets.Label(rect, text);
            Text.Anchor = TextAnchor.UpperLeft;
        }
    }
}
