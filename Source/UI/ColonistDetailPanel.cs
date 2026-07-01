using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using WorkMonitor.Groups;
using WorkMonitor.Tracking;

namespace WorkMonitor.UI
{
    public class ColonistDetailPanel
    {
        private const float RowHeight = 24f;
        private const float ColonistDropdownWidth = 160f;
        private const float ColonistIconSize = 18f;
        private const float ColonistIconGap = 4f;
        private const float ExpandButtonWidth = WorkMonitorTableColumns.ExpandButtonWidth;
        private const float WorkGiverIndent = 16f;
        private const float ExpandAllWidth = 76f;
        private const float HeaderHeight = 18f;
        private const float ChartHeight = 168f;

        private Vector2 scroll;
        private ColonistStats stats;
        private MonitorRangeState boundRangeState;
        private WorkGroupSnapshot returnGroup;
        private WorkGiverDef returnWorkGiver;
        private readonly ColonistWorkTypePieChartPanel pieChartPanel = new ColonistWorkTypePieChartPanel();
        private readonly HashSet<string> expandedGroupKeys = new HashSet<string>();
        private readonly Dictionary<string, ColonistGroupWorkDetail> groupDetailCache = new Dictionary<string, ColonistGroupWorkDetail>();
        private int pendingColonistPawnId;

        public void SetColonist(
            Pawn pawn,
            MonitorRangeState rangeState,
            WorkGroupSnapshot initialGroup = null,
            bool openGroupDetail = false,
            WorkGroupSnapshot returnGroup = null,
            WorkGiverDef returnWorkGiver = null)
        {
            if (pawn == null)
            {
                return;
            }

            SetColonist(pawn.thingIDNumber, rangeState, initialGroup, openGroupDetail, returnGroup, returnWorkGiver);
        }

        public void SetColonist(
            int pawnId,
            MonitorRangeState rangeState,
            WorkGroupSnapshot initialGroup = null,
            bool openGroupDetail = false,
            WorkGroupSnapshot returnGroup = null,
            WorkGiverDef returnWorkGiver = null)
        {
            boundRangeState = rangeState;
            stats = ColonistStatsAggregator.Build(pawnId, rangeState.RangeHours);
            this.returnGroup = returnGroup ?? initialGroup;
            this.returnWorkGiver = returnWorkGiver;
            expandedGroupKeys.Clear();
            groupDetailCache.Clear();

            if (openGroupDetail && initialGroup != null)
            {
                expandedGroupKeys.Add(initialGroup.Key.StorageKey);
            }
        }

        public void Draw(Rect rect, MonitorRangeState rangeState, out bool back, out bool groupClicked, out WorkGroupSnapshot selectedGroup)
        {
            back = false;
            groupClicked = false;
            selectedGroup = null;

            if (stats == null || stats.PawnId <= 0)
            {
                return;
            }

            string backLabel = returnWorkGiver != null
                ? "WorkMonitor.BackToWorkGiver".Translate(WorkGiverLabelUtility.Format(returnWorkGiver))
                : returnGroup != null
                    ? "WorkMonitor.BackToGroup".Translate(returnGroup.Label)
                    : "WorkMonitor.Back".Translate();
            float backWidth = Mathf.Min(Mathf.Max(Text.CalcSize(backLabel).x + 16f, 90f), 200f);

            Text.Font = GameFont.Small;
            if (Widgets.ButtonText(new Rect(rect.x, rect.y, backWidth, 26f), backLabel))
            {
                back = true;
                return;
            }

            Rect portraitRect = new Rect(
                rect.x + backWidth + 6f,
                rect.y + (26f - WorkMonitorUiUtility.ColonistPortraitSize) * 0.5f,
                WorkMonitorUiUtility.ColonistPortraitSize,
                WorkMonitorUiUtility.ColonistPortraitSize);
            WorkMonitorUiUtility.DrawColonistPortrait(portraitRect, stats);

            Rect dropdownRect = new Rect(
                portraitRect.xMax + ColonistIconGap,
                rect.y,
                ColonistDropdownWidth,
                26f);
            int minHour = WorkMonitorUtility.CurrentHourIndex() - rangeState.RangeHours;
            WorkActivityTracker tracker = WorkActivityTracker.EnsureRegistered();
            List<FloatMenuOption> colonistOptions = new List<FloatMenuOption>();
            foreach (int pawnId in ColonistWorkQuery.GetColonistIdsWithAnyWork(minHour))
            {
                int capturedId = pawnId;
                string label = ColonistWorkQuery.ResolveLabel(pawnId, tracker);
                if (ColonistWorkQuery.IsAbsent(pawnId, tracker))
                {
                    label += " *";
                }

                colonistOptions.Add(new FloatMenuOption(label, () => pendingColonistPawnId = capturedId));
            }

            WorkMonitorDropdownUtility.DrawDropdown(dropdownRect, stats.Label, colonistOptions);

            Rect inspectRect = new Rect(
                dropdownRect.xMax + ColonistIconGap,
                rect.y + (26f - ColonistIconSize) * 0.5f,
                ColonistIconSize,
                ColonistIconSize);
            if (!stats.IsAbsent)
            {
                if (Widgets.ButtonImage(inspectRect, TexButton.Info))
                {
                    ColonistInspectUtility.OpenPawnProfile(stats.Pawn);
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

            float toolbarX = inspectRect.xMax + ColonistIconGap;
            Text.Font = GameFont.Tiny;
            WorkMonitorDropdownUtility.DrawRangeDropdown(
                new Rect(toolbarX, rect.y, 110f, 26f),
                rangeState,
                () => SetColonist(stats.PawnId, rangeState, returnGroup: returnGroup, returnWorkGiver: returnWorkGiver));

            if (Widgets.ButtonText(new Rect(toolbarX + 100f, rect.y, 80f, 26f), "WorkMonitor.Refresh".Translate()))
            {
                SetColonist(stats.PawnId, rangeState, returnGroup: returnGroup, returnWorkGiver: returnWorkGiver);
            }

            Rect content = new Rect(rect.x, rect.y + 32f, rect.width, rect.yMax - rect.y - 38f);
            float viewHeight = HeaderHeight + 4f + ChartHeight + 6f + CalculateGroupsViewHeight();
            Rect viewRect = new Rect(0f, 0f, content.width - 16f, viewHeight);
            Widgets.BeginScrollView(content, ref scroll, viewRect);

            float y = 0f;
            DrawHeader(new Rect(0f, y, viewRect.width, HeaderHeight));
            y += HeaderHeight + 4f;

            pieChartPanel.Draw(new Rect(0f, y, viewRect.width, ChartHeight), stats);
            y += ChartHeight + 6f;

            DrawGroupTable(new Rect(0f, y, viewRect.width, viewHeight - y), rangeState, ref y, out groupClicked, out selectedGroup);
            Widgets.EndScrollView();

            if (pendingColonistPawnId > 0)
            {
                HashSet<string> preservedExpansion = new HashSet<string>(expandedGroupKeys);
                int pawnId = pendingColonistPawnId;
                pendingColonistPawnId = 0;
                SetColonist(pawnId, rangeState, returnGroup: returnGroup, returnWorkGiver: returnWorkGiver);
                foreach (string key in preservedExpansion)
                {
                    expandedGroupKeys.Add(key);
                }
            }
        }

        private float CalculateGroupsViewHeight()
        {
            float height = RowHeight * 3f;
            foreach (ColonistGroupStat groupStat in stats.GroupStats)
            {
                height += RowHeight;
                if (expandedGroupKeys.Contains(groupStat.Group.Key.StorageKey))
                {
                    ColonistGroupWorkDetail detail = GetGroupDetail(groupStat.Group, boundRangeState);
                    height += detail.WorkGiverStats.Count * RowHeight;
                }
            }

            return height;
        }

        private ColonistGroupWorkDetail GetGroupDetail(WorkGroupSnapshot group, MonitorRangeState rangeState)
        {
            string key = group.Key.StorageKey;
            if (!groupDetailCache.TryGetValue(key, out ColonistGroupWorkDetail detail))
            {
                detail = ColonistStatsAggregator.BuildGroupDetail(stats.PawnId, group, rangeState.RangeHours);
                groupDetailCache[key] = detail;
            }

            return detail;
        }

        private void DrawHeader(Rect rect)
        {
            Text.Font = GameFont.Tiny;
            string passion = WorkMonitorUiUtility.PassionShort(stats.TopPassion);
            bool showHours = WorkMonitorMod.Settings?.showTimeInHours ?? true;

            string totalSummary = "WorkMonitor.ColonistSummaryWalkWork".Translate(
                passion,
                stats.TotalJobCount,
                WorkMonitorUtility.FormatWorkUnits(stats.TotalWorkUnits),
                WorkMonitorUtility.FormatDuration(stats.TotalTravelTicksSpent, showHours),
                WorkMonitorUtility.FormatDuration(stats.TotalWorkTicksSpent, showHours));
            Widgets.Label(new Rect(rect.x, rect.y, rect.width, 16f), totalSummary);
        }

        private void DrawGroupTable(Rect area, MonitorRangeState rangeState, ref float y, out bool groupClicked, out WorkGroupSnapshot selectedGroup)
        {
            groupClicked = false;
            selectedGroup = null;
            Text.Font = GameFont.Small;
            float tableWidth = area.width;

            string expandLabel = AllGroupsExpanded()
                ? "WorkMonitor.CollapseAll".Translate()
                : "WorkMonitor.ExpandAll".Translate();
            if (Widgets.ButtonText(new Rect(area.xMax - ExpandAllWidth, y, ExpandAllWidth, 22f), expandLabel))
            {
                ToggleExpandAllGroups();
            }

            y += RowHeight;

            Rect titleRect = new Rect(area.x, y, tableWidth, 22f);
            Widgets.Label(titleRect, "WorkMonitor.Groups".Translate());

            y += RowHeight;

            DrawGroupHeader(new Rect(area.x, y, tableWidth, RowHeight));
            y += RowHeight;

            int rowIndex = 0;
            foreach (ColonistGroupStat groupStat in stats.GroupStats)
            {
                string storageKey = groupStat.Group.Key.StorageKey;
                bool expanded = expandedGroupKeys.Contains(storageKey);

                Rect row = new Rect(area.x, y, tableWidth, RowHeight);
                WorkMonitorUiUtility.DrawRowBackground(row, MonitorRowKind.WorkType, rowIndex);

                Rect expandRect = new Rect(row.x, row.y, ExpandButtonWidth, row.height);
                if (Widgets.ButtonText(expandRect, expanded ? "▼" : "▶"))
                {
                    if (expanded)
                    {
                        expandedGroupKeys.Remove(storageKey);
                    }
                    else
                    {
                        expandedGroupKeys.Add(storageKey);
                    }
                }

                Rect clickRect = new Rect(row.x + ExpandButtonWidth, row.y, row.width - ExpandButtonWidth, row.height);
                if (Widgets.ButtonInvisible(clickRect))
                {
                    groupClicked = true;
                    selectedGroup = groupStat.Group;
                }

                DrawGroupRow(row, groupStat);
                y += RowHeight;
                rowIndex++;

                if (expanded)
                {
                    DrawExpandedWorkGivers(area, tableWidth, groupStat.Group, rangeState, ref y, ref rowIndex);
                }
            }
        }

        private bool AllGroupsExpanded()
        {
            if (stats.GroupStats.Count == 0)
            {
                return true;
            }

            foreach (ColonistGroupStat groupStat in stats.GroupStats)
            {
                if (!expandedGroupKeys.Contains(groupStat.Group.Key.StorageKey))
                {
                    return false;
                }
            }

            return true;
        }

        private void ToggleExpandAllGroups()
        {
            if (AllGroupsExpanded())
            {
                expandedGroupKeys.Clear();
                return;
            }

            foreach (ColonistGroupStat groupStat in stats.GroupStats)
            {
                expandedGroupKeys.Add(groupStat.Group.Key.StorageKey);
            }
        }

        private void DrawExpandedWorkGivers(Rect area, float tableWidth, WorkGroupSnapshot group, MonitorRangeState rangeState, ref float y, ref int rowIndex)
        {
            ColonistGroupWorkDetail detail = GetGroupDetail(group, rangeState);
            bool showHours = WorkMonitorMod.Settings?.showTimeInHours ?? true;

            foreach (ColonistWorkGiverStat wg in detail.WorkGiverStats)
            {
                Rect columnRow = new Rect(area.x, y, tableWidth, RowHeight);
                WorkMonitorUiUtility.DrawRowBackground(columnRow, MonitorRowKind.WorkGiver, rowIndex);

                WorkMonitorTableColumns.GetColonistGroupColumns(
                    columnRow,
                    out Rect interestCol,
                    out Rect jobsCol,
                    out Rect endlessCol,
                    out Rect workCol,
                    out Rect walkCol,
                    out Rect activeWorkCol,
                    out Rect shareCol);

                Text.Font = GameFont.Tiny;
                Color prev = GUI.color;
                GUI.color = new Color(0.8f, 0.8f, 0.8f);
                float labelWidth = interestCol.x - area.x - WorkGiverIndent - 8f;
                float labelLeft = area.x + WorkGiverIndent;
                WorkGiverLabelUtility.Draw(columnRow, labelLeft, labelWidth, wg.WorkGiver, GameFont.Tiny);
                GUI.color = prev;

                WorkMonitorUiUtility.DrawInterestValue(
                    interestCol,
                    ColonistWorkQuery.FormatColonistWorkGiverInterest(stats.PawnId, wg.WorkGiver, group));

                WorkMonitorUiUtility.LabelRightWorkGiverStatValue(jobsCol, wg.JobCount.ToString());
                WorkMonitorUiUtility.LabelRightWorkGiverStatValue(endlessCol, wg.EndlessJobCount.ToString());
                WorkMonitorUiUtility.LabelRightWorkGiverStatValue(workCol, WorkMonitorUtility.FormatWorkUnits(wg.WorkUnitsSpent));
                WorkMonitorUiUtility.LabelRightWorkGiverStatValue(walkCol, WorkMonitorUtility.FormatDuration(wg.TravelTicksSpent, showHours));
                WorkMonitorUiUtility.LabelRightWorkGiverStatValue(activeWorkCol, WorkMonitorUtility.FormatDuration(wg.WorkTicksSpent, showHours));
                WorkMonitorUiUtility.LabelRightWorkGiverStatValue(shareCol, WorkMonitorUiUtility.FormatTimeShare(wg.TicksSpent, stats.TotalTicksSpent));

                y += RowHeight;
                rowIndex++;
            }
        }

        private static void DrawGroupHeader(Rect row)
        {
            Text.Font = GameFont.Tiny;
            Color prev = GUI.color;
            GUI.color = new Color(0.72f, 0.72f, 0.72f);

            float metricsLeft = WorkMonitorTableColumns.ColonistGroupMetricsLeftEdge(row);
            float labelLeft = row.x + ExpandButtonWidth + PieChartPalette.SwatchTotalWidth;
            Widgets.Label(new Rect(labelLeft, row.y, metricsLeft - labelLeft - 8f, row.height), "WorkMonitor.Group".Translate());
            WorkMonitorTableColumns.GetColonistGroupColumns(row, out Rect interestCol, out Rect jobCol, out Rect endlessCol, out Rect workCol, out Rect walkCol, out Rect activeWorkCol, out Rect shareCol);
            LabelRight(interestCol, "WorkMonitor.Interest".Translate());
            TooltipHandler.TipRegion(interestCol, "WorkMonitor.ColonistInterestTip".Translate());
            LabelRight(jobCol, "WorkMonitor.Jobs".Translate());
            LabelRight(endlessCol, "WorkMonitor.EndlessJobs".Translate());
            TooltipHandler.TipRegion(endlessCol, "WorkMonitor.EndlessJobsTip".Translate());
            LabelRight(workCol, "WorkMonitor.Work".Translate());
            TooltipHandler.TipRegion(workCol, "WorkMonitor.WorkEstimatedTip".Translate());
            LabelRight(walkCol, "WorkMonitor.Walk".Translate());
            LabelRight(activeWorkCol, "WorkMonitor.WorkTime".Translate());
            LabelRight(shareCol, "WorkMonitor.TimeShare".Translate());
            TooltipHandler.TipRegion(shareCol, "WorkMonitor.TimeShareTip".Translate());

            GUI.color = prev;
        }

        private void DrawGroupRow(Rect row, ColonistGroupStat groupStat)
        {
            Text.Font = GameFont.Small;
            float metricsLeft = WorkMonitorTableColumns.ColonistGroupMetricsLeftEdge(row);
            float swatchLeft = row.x + ExpandButtonWidth;
            float labelLeft = swatchLeft + PieChartPalette.SwatchTotalWidth;
            bool showSwatch = groupStat.JobCount > 0 || groupStat.WorkUnitsSpent > 0f || groupStat.TicksSpent > 0;
            PieChartPalette.DrawSwatch(
                row,
                swatchLeft,
                showSwatch ? PieChartPalette.ForWorkGroup(groupStat.Group.Key) : (Color?)null);
            Widgets.Label(
                new Rect(labelLeft, row.y, metricsLeft - labelLeft - 8f, row.height),
                groupStat.Group.Label.Truncate(metricsLeft - labelLeft - 8f));
            WorkMonitorTableColumns.GetColonistGroupColumns(row, out Rect interestCol, out Rect jobCol, out Rect endlessCol, out Rect workCol, out Rect walkCol, out Rect activeWorkCol, out Rect shareCol);
            bool showHours = WorkMonitorMod.Settings?.showTimeInHours ?? true;

            WorkMonitorUiUtility.DrawInterestValue(
                interestCol,
                ColonistWorkQuery.FormatColonistSkillGroupInterest(stats.PawnId, groupStat.Group));
            WorkMonitorUiUtility.LabelRightStatValue(jobCol, groupStat.JobCount.ToString());
            WorkMonitorUiUtility.LabelRightStatValue(endlessCol, groupStat.EndlessJobCount.ToString());
            WorkMonitorUiUtility.LabelRightStatValue(workCol, WorkMonitorUtility.FormatWorkUnits(groupStat.WorkUnitsSpent));
            WorkMonitorUiUtility.LabelRightStatValue(walkCol, WorkMonitorUtility.FormatDuration(groupStat.TravelTicksSpent, showHours));
            WorkMonitorUiUtility.LabelRightStatValue(activeWorkCol, WorkMonitorUtility.FormatDuration(groupStat.WorkTicksSpent, showHours));
            WorkMonitorUiUtility.LabelRightStatValue(shareCol, WorkMonitorUiUtility.FormatTimeShare(groupStat.TicksSpent, stats.TotalTicksSpent));
        }

        private static void LabelRight(Rect rect, string text)
        {
            Text.Anchor = TextAnchor.MiddleRight;
            Widgets.Label(rect, text);
            Text.Anchor = TextAnchor.UpperLeft;
        }
    }
}
