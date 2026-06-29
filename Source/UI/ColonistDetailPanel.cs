using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using WorkMonitor.Groups;

namespace WorkMonitor.UI
{
    public class ColonistDetailPanel
    {
        private const float RowHeight = 24f;

        private Vector2 scroll;
        private ColonistStats stats;

        public void SetColonist(Pawn pawn)
        {
            stats = ColonistStatsAggregator.Build(pawn);
        }

        public void Draw(Rect rect, out bool back)
        {
            back = false;

            if (stats?.Pawn == null)
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
                new Rect(rect.x + 76f, rect.y + 2f, rect.width - 76f, 22f),
                "WorkMonitor.ColonistDetailTitle".Translate(stats.Label));

            Rect header = new Rect(rect.x, rect.y + 32f, rect.width, 18f);
            DrawHeader(header);

            Rect content = new Rect(rect.x, header.yMax + 8f, rect.width, rect.yMax - header.yMax - 12f);
            float viewHeight = RowHeight * 2f + stats.GroupStats.Count * RowHeight;
            Rect view = new Rect(0f, 0f, content.width - 16f, viewHeight);
            Widgets.BeginScrollView(content, ref scroll, view);

            float y = 0f;
            DrawGroupTable(new Rect(0f, y, view.width, viewHeight), ref y);

            Widgets.EndScrollView();
        }

        private void DrawHeader(Rect rect)
        {
            Text.Font = GameFont.Tiny;
            string passion = WorkMonitorUiUtility.PassionShort(stats.TopPassion);
            string summary = "WorkMonitor.ColonistSummary".Translate(
                passion,
                stats.TotalJobCount,
                WorkMonitorUtility.FormatWorkUnits(stats.TotalWorkUnits),
                WorkMonitorUtility.FormatDuration(stats.TotalTicksSpent, WorkMonitorMod.Settings?.showTimeInHours ?? true));
            Widgets.Label(new Rect(rect.x, rect.y, rect.width, 16f), summary);
        }

        private void DrawGroupTable(Rect area, ref float y)
        {
            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(area.x, y, area.width, 22f), "WorkMonitor.Groups".Translate());
            y += RowHeight;

            DrawGroupHeader(new Rect(area.x, y, area.width, RowHeight));
            y += RowHeight;

            int rowIndex = 0;
            foreach (ColonistGroupStat groupStat in stats.GroupStats)
            {
                Rect row = new Rect(area.x, y, area.width, RowHeight);
                if (rowIndex % 2 == 1)
                {
                    Widgets.DrawBoxSolid(row, new Color(1f, 1f, 1f, 0.03f));
                }

                DrawGroupRow(row, groupStat);
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
            Widgets.Label(new Rect(row.x, row.y, metricsLeft - row.x - 8f, row.height), "WorkMonitor.Group".Translate());
            WorkMonitorTableColumns.GetColonistGroupColumns(row, out Rect jobCol, out Rect workCol, out Rect timeCol, out Rect shareCol);
            LabelRight(jobCol, "WorkMonitor.Jobs".Translate());
            LabelRight(workCol, "WorkMonitor.Work".Translate());
            LabelRight(timeCol, "WorkMonitor.Time".Translate());
            LabelRight(shareCol, "WorkMonitor.TimeShare".Translate());

            GUI.color = prev;
        }

        private static void DrawGroupRow(Rect row, ColonistGroupStat groupStat)
        {
            Text.Font = GameFont.Small;
            float metricsLeft = WorkMonitorTableColumns.ColonistGroupMetricsLeftEdge(row);
            Widgets.Label(new Rect(row.x, row.y, metricsLeft - row.x - 8f, row.height), groupStat.Group.Label.Truncate(metricsLeft - row.x - 8f));
            WorkMonitorTableColumns.GetColonistGroupColumns(row, out Rect jobCol, out Rect workCol, out Rect timeCol, out Rect shareCol);

            LabelRight(jobCol, groupStat.JobCount.ToString());
            LabelRight(workCol, WorkMonitorUtility.FormatWorkUnits(groupStat.WorkUnitsSpent));
            LabelRight(
                timeCol,
                WorkMonitorUtility.FormatDuration(groupStat.TicksSpent, WorkMonitorMod.Settings?.showTimeInHours ?? true));
            LabelRight(shareCol, WorkMonitorUiUtility.FormatTimeShare(groupStat.TicksSpent, groupStat.GroupTicksSpent));
        }

        private static void LabelRight(Rect rect, string text)
        {
            Text.Anchor = TextAnchor.MiddleRight;
            Widgets.Label(rect, text);
            Text.Anchor = TextAnchor.UpperLeft;
        }
    }
}
