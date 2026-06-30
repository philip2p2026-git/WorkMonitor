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

        private Vector2 scroll;
        private ColonistStats stats;
        private MonitorRangeState boundRangeState;
        private WorkGroupSnapshot returnGroup;
        private WorkGiverDef returnWorkGiver;
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

            Rect dropdownRect = new Rect(rect.x + backWidth + 6f, rect.y, ColonistDropdownWidth, 26f);
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

            Rect header = new Rect(rect.x, rect.y + 32f, rect.width, 18f);
            DrawHeader(header);

            Rect content = new Rect(rect.x, header.yMax + 8f, rect.width, rect.yMax - header.yMax - 12f);
            float viewHeight = CalculateGroupsViewHeight();
            Rect viewRect = new Rect(0f, 0f, content.width - 16f, viewHeight);
            Widgets.BeginScrollView(content, ref scroll, viewRect);
            float y = 0f;
            DrawGroupTable(new Rect(0f, y, viewRect.width, viewHeight), rangeState, ref y, out groupClicked, out selectedGroup);
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
            float height = RowHeight * 2f;
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
            Rect titleRect = new Rect(area.x, y, area.width - ExpandAllWidth - 4f, 22f);
            Widgets.Label(titleRect, "WorkMonitor.Groups".Translate());
            string expandLabel = AllGroupsExpanded()
                ? "WorkMonitor.CollapseAll".Translate()
                : "WorkMonitor.ExpandAll".Translate();
            if (Widgets.ButtonText(new Rect(area.xMax - ExpandAllWidth, y, ExpandAllWidth, 22f), expandLabel))
            {
                ToggleExpandAllGroups();
            }
            y += RowHeight;

            DrawGroupHeader(new Rect(area.x, y, area.width, RowHeight));
            y += RowHeight;

            int rowIndex = 0;
            foreach (ColonistGroupStat groupStat in stats.GroupStats)
            {
                string storageKey = groupStat.Group.Key.StorageKey;
                bool expanded = expandedGroupKeys.Contains(storageKey);

                Rect row = new Rect(area.x, y, area.width, RowHeight);
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
                    DrawExpandedWorkGivers(area, groupStat.Group, rangeState, ref y, ref rowIndex);
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

        private void DrawExpandedWorkGivers(Rect area, WorkGroupSnapshot group, MonitorRangeState rangeState, ref float y, ref int rowIndex)
        {
            ColonistGroupWorkDetail detail = GetGroupDetail(group, rangeState);
            bool showHours = WorkMonitorMod.Settings?.showTimeInHours ?? true;
            Rect columnRow = new Rect(area.x, y, area.width, RowHeight);

            foreach (ColonistWorkGiverStat wg in detail.WorkGiverStats)
            {
                Rect row = new Rect(area.x + WorkGiverIndent, y, area.width - WorkGiverIndent, RowHeight);
                WorkMonitorUiUtility.DrawRowBackground(row, MonitorRowKind.WorkGiver, rowIndex);

                columnRow.y = y;
                float metricsLeft = WorkMonitorTableColumns.ColonistWorkGiverMetricsLeftEdge(columnRow);
                Text.Font = GameFont.Tiny;
                Color prev = GUI.color;
                GUI.color = new Color(0.8f, 0.8f, 0.8f);
                Widgets.Label(new Rect(area.x + WorkGiverIndent, row.y, metricsLeft - area.x - WorkGiverIndent - 8f, row.height), wg.Label.Truncate(metricsLeft - area.x - WorkGiverIndent - 8f));
                GUI.color = prev;
                Text.Font = GameFont.Small;

                WorkMonitorTableColumns.GetColonistWorkGiverColumns(columnRow, out Rect jobCol, out Rect endlessCol, out Rect workCol, out Rect walkCol, out Rect activeWorkCol, out Rect shareCol);
                LabelRight(jobCol, wg.JobCount.ToString());
                LabelRight(endlessCol, wg.EndlessJobCount.ToString());
                LabelRight(workCol, WorkMonitorUtility.FormatWorkUnits(wg.WorkUnitsSpent));
                LabelRight(walkCol, WorkMonitorUtility.FormatDuration(wg.TravelTicksSpent, showHours));
                LabelRight(activeWorkCol, WorkMonitorUtility.FormatDuration(wg.WorkTicksSpent, showHours));
                LabelRight(shareCol, WorkMonitorUiUtility.FormatTimeShare(wg.TicksSpent, stats.TotalTicksSpent));

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
            Widgets.Label(new Rect(row.x + ExpandButtonWidth, row.y, metricsLeft - row.x - ExpandButtonWidth - 8f, row.height), "WorkMonitor.Group".Translate());
            WorkMonitorTableColumns.GetColonistGroupColumns(row, out Rect jobCol, out Rect endlessCol, out Rect workCol, out Rect walkCol, out Rect activeWorkCol, out Rect shareCol);
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
            Widgets.Label(new Rect(row.x + ExpandButtonWidth, row.y, metricsLeft - row.x - ExpandButtonWidth - 8f, row.height), groupStat.Group.Label.Truncate(metricsLeft - row.x - ExpandButtonWidth - 8f));
            WorkMonitorTableColumns.GetColonistGroupColumns(row, out Rect jobCol, out Rect endlessCol, out Rect workCol, out Rect walkCol, out Rect activeWorkCol, out Rect shareCol);
            bool showHours = WorkMonitorMod.Settings?.showTimeInHours ?? true;

            LabelRight(jobCol, groupStat.JobCount.ToString());
            LabelRight(endlessCol, groupStat.EndlessJobCount.ToString());
            LabelRight(workCol, WorkMonitorUtility.FormatWorkUnits(groupStat.WorkUnitsSpent));
            LabelRight(walkCol, WorkMonitorUtility.FormatDuration(groupStat.TravelTicksSpent, showHours));
            LabelRight(activeWorkCol, WorkMonitorUtility.FormatDuration(groupStat.WorkTicksSpent, showHours));
            LabelRight(shareCol, WorkMonitorUiUtility.FormatTimeShare(groupStat.TicksSpent, stats.TotalTicksSpent));
        }

        private static void LabelRight(Rect rect, string text)
        {
            Text.Anchor = TextAnchor.MiddleRight;
            Widgets.Label(rect, text);
            Text.Anchor = TextAnchor.UpperLeft;
        }
    }
}
