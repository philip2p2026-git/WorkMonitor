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

        private static bool WorkGiverFirst => WorkMonitorMod.Settings?.groupDetailWorkGiverFirst ?? false;

        public void ClearExpandCaches()
        {
            expandedGroupKeys.Clear();
            expandedLevel2Keys.Clear();
            colonistDetailCache.Clear();
            workGiverDetailCache.Clear();
            activeWorkGiversCache.Clear();
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

            string expandLabel = BulkExpandUtility.BulkButtonLabel(AllLevel2Expanded());
            if (Widgets.ButtonText(new Rect(toolbarRight - ExpandAllWidth, rect.y, ExpandAllWidth, 24f), expandLabel))
            {
                ApplyBulkExpandToggle();
                TooltipHandler.TipRegion(new Rect(toolbarRight - ExpandAllWidth, rect.y, ExpandAllWidth, 24f), BulkExpandTooltip());
            }
            else
            {
                TooltipHandler.TipRegion(new Rect(toolbarRight - ExpandAllWidth, rect.y, ExpandAllWidth, 24f), BulkExpandTooltip());
            }

            toolbarRight -= ExpandAllWidth + ToolbarGap;

            string layoutLabel = WorkGiverFirst
                ? "WorkMonitor.GroupByWorkGiver".Translate()
                : "WorkMonitor.GroupByColonist".Translate();
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
                    foreach (ColonistWorkStat colonist in stats.ColonistStats)
                    {
                        if (expandedLevel2Keys.Contains(Level2PawnKey(storageKey, colonist.PawnId)))
                        {
                            ColonistGroupWorkDetail detail = GetColonistDetail(stats.Group, colonist.PawnId, boundRangeState);
                            height += detail.WorkGiverStats.Count * RowHeight;
                        }
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

            var ranked = new List<(WorkGiverDef workGiver, int ticks)>();
            foreach (WorkGiverDef workGiver in stats.Group.WorkGivers)
            {
                WorkGiverDetailStats detail = GetWorkGiverDetail(stats.Group, workGiver, rangeState);
                if (detail == null || detail.ColonistStats.Count == 0)
                {
                    continue;
                }

                int ticks = detail.ColonistStats.Sum(c => c.TicksSpent);
                ranked.Add((workGiver, ticks));
            }

            cached = ranked
                .OrderByDescending(entry => entry.ticks)
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

            settings.groupDetailWorkGiverFirst = !settings.groupDetailWorkGiverFirst;
            ClearExpandCaches();
        }

        private void ApplyBulkExpandToggle()
        {
            BulkExpandUtility.ApplyBulkToggle(
                AllLevel2Expanded(),
                ExpandOneLevel,
                CollapseOneLevel);
        }

        private void ExpandOneLevel()
        {
            BulkExpandUtility.ExpandOneLevel(AllLevel1Expanded(), ExpandAllLevel1, ExpandAllLevel2);
        }

        private void CollapseOneLevel()
        {
            BulkExpandUtility.CollapseOneLevel(AnyLevel2Expanded(), CollapseAllLevel2, CollapseAllLevel1);
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

        private bool AllLevel2Expanded()
        {
            if (!AllLevel1Expanded())
            {
                return false;
            }

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
                        if (!expandedLevel2Keys.Contains(Level2WorkGiverKey(storageKey, workGiver.defName)))
                        {
                            return false;
                        }
                    }
                }
                else
                {
                    foreach (ColonistWorkStat colonist in stats.ColonistStats)
                    {
                        if (!expandedLevel2Keys.Contains(Level2PawnKey(storageKey, colonist.PawnId)))
                        {
                            return false;
                        }
                    }
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
                }
            }
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
