using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using WorkMonitor.Groups;
using WorkMonitor.Tracking;

namespace WorkMonitor.UI
{
    public class WorkGiverDetailPanel
    {
        private const float RowHeight = 24f;
        private const float ChartHeight = 168f;
        private const float ColonistIconSize = WorkMonitorTableColumns.ColonistIconSize;
        private const float JobsWidth = 63f;
        private const float WorkWidth = 78f;
        private const float ColumnGap = 10f;
        private const float WorkGiverDropdownWidth = 180f;

        private readonly WorkGroupChartPanel chartPanel = new WorkGroupChartPanel();
        private Vector2 scroll;
        private WorkGiverDetailStats stats;
        private WorkGiverDef pendingWorkGiverSelection;

        public WorkGroupSnapshot CurrentGroup => stats?.Group;
        public WorkGiverDef CurrentWorkGiver => stats?.WorkGiver;

        public void SetWorkGiver(WorkGroupSnapshot group, WorkGiverDef workGiver, MonitorRangeState rangeState)
        {
            stats = WorkGiverStatsAggregator.Build(group, workGiver, rangeState.RangeHours);
        }

        public void Draw(Rect rect, MonitorRangeState rangeState, out bool back, out bool colonistClicked, out ColonistWorkStat selectedColonist)
        {
            back = false;
            colonistClicked = false;
            selectedColonist = null;

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

            List<FloatMenuOption> wgOptions = WorkMonitorDropdownUtility.BuildOptions(
                stats.Group.WorkGivers,
                wg => WorkGiverLabelUtility.Format(wg),
                wg => pendingWorkGiverSelection = wg);
            WorkMonitorDropdownUtility.DrawDropdown(
                new Rect(rect.x + 76f, rect.y, WorkGiverDropdownWidth, 26f),
                stats.Label,
                wgOptions);

            float toolbarX = rect.x + 76f + WorkGiverDropdownWidth + 6f;
            Text.Font = GameFont.Tiny;
            WorkMonitorDropdownUtility.DrawRangeDropdown(
                new Rect(toolbarX, rect.y, 110f, 26f),
                rangeState,
                () => SetWorkGiver(stats.Group, stats.WorkGiver, rangeState));

            if (Widgets.ButtonText(new Rect(toolbarX + 100f, rect.y, 80f, 26f), "WorkMonitor.Refresh".Translate()))
            {
                SetWorkGiver(stats.Group, stats.WorkGiver, rangeState);
            }

            Rect header = new Rect(rect.x, rect.y + 32f, rect.width, 36f);
            DrawHeader(header);

            Rect chartRect = new Rect(rect.x, header.yMax + 4f, rect.width, ChartHeight);
            DrawCharts(chartRect, rangeState);

            Rect content = new Rect(rect.x, chartRect.yMax + 6f, rect.width, rect.yMax - chartRect.yMax - 12f);
            float viewHeight = 90f + stats.ColonistStats.Count * RowHeight;
            Rect view = new Rect(0f, 0f, content.width - 16f, viewHeight);
            Widgets.BeginScrollView(content, ref scroll, view);

            float y = 0f;
            DrawColonistTable(new Rect(0f, y, view.width, viewHeight - y), ref y, out colonistClicked, out selectedColonist);
            y += 12f;
            DrawMapRow(new Rect(0f, y, view.width, RowHeight));

            Widgets.EndScrollView();

            if (pendingWorkGiverSelection != null)
            {
                SetWorkGiver(stats.Group, pendingWorkGiverSelection, rangeState);
                pendingWorkGiverSelection = null;
            }
        }

        private void DrawHeader(Rect rect)
        {
            Text.Font = GameFont.Tiny;
            string totals = "WorkMonitor.JobsWorkWalkActiveSummary".Translate(
                stats.TotalJobCount,
                WorkMonitorUtility.FormatWorkUnits(stats.TotalWorkUnits),
                WorkMonitorUtility.FormatDuration(stats.TotalTravelTicks, WorkMonitorMod.Settings?.showTimeInHours ?? true),
                WorkMonitorUtility.FormatDuration(stats.TotalWorkTicks, WorkMonitorMod.Settings?.showTimeInHours ?? true));
            Widgets.Label(new Rect(rect.x, rect.y, rect.width, 16f), totals);

            string mapSummary = "WorkMonitor.MapBacklogRow".Translate(
                WorkMonitorUiUtility.FormatMapOpenTasks(stats.MapOpenTasks, stats.MapNewTodayOpenTasks),
                WorkMonitorUiUtility.FormatMapWorkLeft(stats.MapWorkLeft, stats.MapNewTodayWorkLeft),
                WorkMonitorUtility.FormatGameDateTime(stats.MapSampleTick),
                WorkMonitorUtility.FormatSampleAge(stats.MapSampleTick));
            Widgets.Label(new Rect(rect.x, rect.y + 18f, rect.width, 16f), mapSummary);
        }

        private void DrawCharts(Rect rect, MonitorRangeState rangeState)
        {
            WorkActivityTracker tracker = WorkActivityTracker.EnsureRegistered();
            WorkHistoryTierBuffer history = tracker?.GetGroupHistory(stats.Group.Key.StorageKey);
            int minHour = rangeState.MinHourIndex;
            int rangeHours = rangeState.RangeHours;
            string wgDefName = stats.WorkGiver.defName;

            float gap = 8f;
            float cellW = (rect.width - gap) / 2f;

            WorkChartDataBuilder.BuildJobCountSeries(history, minHour, rangeHours, rangeState.UsesHourlyChart, out float[] colonistJobs, out string[] jobLabels);
            WorkChartDataBuilder.BuildMapOpenTasksSeriesForWorkGiver(wgDefName, minHour, rangeHours, out float[] mapJobs, out float[] mapNewJobs, out _);

            DualStreamChart.Draw(new Rect(rect.x, rect.y, cellW, rect.height), colonistJobs, mapJobs, mapNewJobs, jobLabels,
                "WorkMonitor.MetricJobCount".Translate(), "WorkMonitor.JobProcessed".Translate(), "WorkMonitor.ExistJob".Translate(),
                "WorkMonitor.ChartExistJob".Translate(), "WorkMonitor.ChartNewJobToday".Translate());

            WorkChartDataBuilder.BuildWorkUnitsSeries(history, minHour, rangeHours, rangeState.UsesHourlyChart, out float[] colonistWork, out string[] workLabels);
            WorkChartDataBuilder.BuildMapWorkLeftSeriesForWorkGiver(wgDefName, minHour, rangeHours, out float[] mapWork, out float[] mapNewWork, out _);

            DualStreamChart.Draw(new Rect(rect.x + cellW + gap, rect.y, cellW, rect.height), colonistWork, mapWork, mapNewWork, workLabels,
                "WorkMonitor.MetricWorkUnits".Translate(), "WorkMonitor.WorkProcessed".Translate(), "WorkMonitor.ExistWork".Translate(),
                "WorkMonitor.ChartExistWork".Translate(), "WorkMonitor.ChartNewWorkToday".Translate());
        }

        private void DrawColonistTable(Rect area, ref float y, out bool colonistClicked, out ColonistWorkStat selectedColonist)
        {
            colonistClicked = false;
            selectedColonist = null;

            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(area.x, y, area.width, 22f), "WorkMonitor.Colonists".Translate());
            y += RowHeight;

            DrawColonistHeader(new Rect(area.x, y, area.width, RowHeight));
            y += RowHeight;

            int rowIndex = 0;
            foreach (ColonistWorkStat colonist in stats.ColonistStats)
            {
                Rect row = new Rect(area.x, y, area.width, RowHeight);
                WorkMonitorUiUtility.DrawRowBackground(row, MonitorRowKind.Colonist, rowIndex);

                GetColonistTableColumns(row, out Rect portraitCol, out Rect iconsCol, out Rect labelCol, out _, out _, out _, out _, out _, out _, out _, out _);
                Rect clickRect = new Rect(labelCol.x, row.y, row.xMax - labelCol.x, row.height);
                if (Widgets.ButtonInvisible(clickRect))
                {
                    colonistClicked = true;
                    selectedColonist = colonist;
                }

                DrawColonistRow(row, colonist, iconsCol, stats);
                y += RowHeight;
                rowIndex++;
            }
        }

        private void DrawMapRow(Rect row)
        {
            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(row.x, row.y, row.width * 0.4f, row.height), "WorkMonitor.WorkGiversMap".Translate());
            string jobsText = WorkMonitorUiUtility.FormatMapOpenTasks(stats.MapOpenTasks, stats.MapNewTodayOpenTasks);
            string workText = WorkMonitorUiUtility.FormatMapWorkLeft(stats.MapWorkLeft, stats.MapNewTodayWorkLeft);
            WorkMonitorUiUtility.LabelRightStatValue(new Rect(row.xMax - WorkWidth - ColumnGap - JobsWidth, row.y, JobsWidth, row.height), jobsText);
            WorkMonitorUiUtility.LabelRightStatValue(new Rect(row.xMax - WorkWidth, row.y, WorkWidth, row.height), workText);
        }

        private static void DrawColonistHeader(Rect row)
        {
            Text.Font = GameFont.Tiny;
            Color prev = GUI.color;
            GUI.color = new Color(0.72f, 0.72f, 0.72f);
            GetColonistTableColumns(row, out Rect portraitCol, out Rect iconsCol, out Rect labelCol, out Rect interestCol, out Rect kpiJobCol, out Rect kpiWorkCol, out Rect jobsCol, out Rect endlessCol, out Rect workCol, out Rect walkCol, out Rect activeWorkCol);
            Widgets.Label(portraitCol, "");
            Widgets.Label(labelCol, "WorkMonitor.Colonist".Translate());
            Widgets.Label(iconsCol, "");
            LabelRight(interestCol, "WorkMonitor.Interest".Translate());
            TooltipHandler.TipRegion(interestCol, "WorkMonitor.ColonistInterestTip".Translate());
            LabelRight(kpiJobCol, "WorkMonitor.KpiJobs".Translate());
            LabelRight(kpiWorkCol, "WorkMonitor.KpiWork".Translate());
            LabelRight(jobsCol, "WorkMonitor.Jobs".Translate());
            LabelRight(endlessCol, "WorkMonitor.EndlessJobs".Translate());
            LabelRight(workCol, "WorkMonitor.Work".Translate());
            LabelRight(walkCol, "WorkMonitor.Walk".Translate());
            LabelRight(activeWorkCol, "WorkMonitor.WorkTime".Translate());
            GUI.color = prev;
        }

        private void DrawColonistRow(Rect row, ColonistWorkStat colonist, Rect iconsCol, WorkGiverDetailStats detail)
        {
            Text.Font = GameFont.Small;
            GetColonistTableColumns(row, out Rect portraitCol, out _, out Rect labelCol, out Rect interestCol, out Rect kpiJobCol, out Rect kpiWorkCol, out Rect jobsCol, out Rect endlessCol, out Rect workCol, out Rect walkCol, out Rect activeWorkCol);
            WorkMonitorUiUtility.DrawColonistPortrait(portraitCol, colonist);
            Rect inspectRect = new Rect(iconsCol.x, row.y + (row.height - ColonistIconSize) * 0.5f, ColonistIconSize, ColonistIconSize);
            if (!colonist.IsAbsent)
            {
                if (Widgets.ButtonImage(inspectRect, TexButton.Info))
                {
                    ColonistInspectUtility.OpenPawnProfile(colonist.Pawn);
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

            WorkMonitorUiUtility.DrawColonistLabel(labelCol, colonist);
            WorkMonitorUiUtility.DrawInterestValue(
                interestCol,
                ColonistWorkQuery.FormatColonistWorkGiverInterest(colonist.PawnId, detail.WorkGiver, detail.Group));
            WorkMonitorUiUtility.LabelRightStatValue(kpiJobCol, FormatPerHour(colonist.JobsPerHour, integer: true));
            WorkMonitorUiUtility.LabelRightStatValue(kpiWorkCol, FormatPerHour(colonist.WorkUnitsPerHour, integer: false));
            WorkMonitorUiUtility.LabelRightStatValue(jobsCol, colonist.JobCount.ToString());
            WorkMonitorUiUtility.LabelRightStatValue(endlessCol, colonist.EndlessJobCount.ToString());
            WorkMonitorUiUtility.LabelRightStatValue(workCol, WorkMonitorUtility.FormatWorkUnits(colonist.WorkUnitsSpent));
            WorkMonitorUiUtility.LabelRightStatValue(walkCol, WorkMonitorUtility.FormatDuration(colonist.TravelTicksSpent, WorkMonitorMod.Settings?.showTimeInHours ?? true));
            WorkMonitorUiUtility.LabelRightStatValue(activeWorkCol, WorkMonitorUtility.FormatDuration(colonist.WorkTicksSpent, WorkMonitorMod.Settings?.showTimeInHours ?? true));
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
            WorkMonitorTableColumns.GetWorkGiverDetailColonistTableColumns(
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
            if (value <= 0f) return "—";
            string formatted = integer ? value.ToString("0.#") : WorkMonitorUtility.FormatWorkUnits(value);
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
