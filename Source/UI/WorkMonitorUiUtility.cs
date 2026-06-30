using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using WorkMonitor.Groups;

namespace WorkMonitor.UI
{
    public static class WorkMonitorUiUtility
    {
        public static Color StatusColor(WorkActivityStatus status)
        {
            return status switch
            {
                WorkActivityStatus.Green => new Color(0.35f, 0.85f, 0.4f),
                WorkActivityStatus.Yellow => new Color(0.95f, 0.8f, 0.2f),
                WorkActivityStatus.Grey => new Color(0.55f, 0.55f, 0.55f),
                _ => new Color(0.9f, 0.35f, 0.35f)
            };
        }

        public static string PassionLabel(Passion passion)
        {
            return passion switch
            {
                Passion.Major => "WorkMonitor.PassionMajor".Translate(),
                Passion.Minor => "WorkMonitor.PassionMinor".Translate(),
                _ => "WorkMonitor.PassionNone".Translate()
            };
        }

        public static string PassionShort(Passion passion)
        {
            return passion switch
            {
                Passion.Major => "++",
                Passion.Minor => "+",
                _ => ""
            };
        }

        public static int TotalInterestedCount(WorkGroupStats stats)
        {
            return stats.MajorInterestCount + stats.MinorInterestCount;
        }

        public static string FormatInterestRatio(WorkGroupStats stats)
        {
            int major = stats.MajorInterestCount;
            int total = TotalInterestedCount(stats);
            if (total <= 0)
            {
                return "—";
            }

            return major + "++/" + total + "+";
        }

        public static void DrawShareBar(Rect rect, float percent)
        {
            Widgets.DrawBoxSolid(rect, new Color(0.15f, 0.15f, 0.15f, 0.5f));
            Rect fill = rect;
            fill.width *= Mathf.Clamp01(percent / 100f);
            Widgets.DrawBoxSolid(fill, new Color(0.35f, 0.7f, 0.95f, 0.85f));
        }

        public static string FormatWithShare(int value, int total)
        {
            if (total <= 0 || value <= 0)
            {
                return value.ToString();
            }

            int pct = Mathf.RoundToInt(value * 100f / total);
            return value + " (" + pct + "%)";
        }

        public static string FormatWorkWithShare(float value, float total)
        {
            string formatted = WorkMonitorUtility.FormatWorkUnits(value);
            if (total <= 0f || value <= 0f)
            {
                return formatted;
            }

            int pct = Mathf.RoundToInt(value * 100f / total);
            return formatted + " (" + pct + "%)";
        }

        public static string FormatTimeShare(int pawnTicks, int groupTicks)
        {
            if (groupTicks <= 0 || pawnTicks <= 0)
            {
                return "—";
            }

            return Mathf.RoundToInt(pawnTicks * 100f / groupTicks) + "%";
        }

        public static string FormatMapOpenTasks(int total, int newToday)
        {
            if (newToday <= 0)
            {
                return total.ToString();
            }

            return total + "(" + newToday + ")";
        }

        public static string FormatMapWorkLeft(float total, float newToday)
        {
            if (total <= 0f)
            {
                return "—";
            }

            if (newToday <= 0f)
            {
                return WorkMonitorUtility.FormatWorkUnits(total);
            }

            return WorkMonitorUtility.FormatWorkUnits(total) + "(" + WorkMonitorUtility.FormatWorkUnits(newToday) + ")";
        }

        public const float AbsentIconSize = 14f;

        public static void DrawColonistLabel(Rect labelCol, ColonistWorkStat colonist)
        {
            string passion = PassionShort(colonist.Passion);
            string nameText = (passion + " " + colonist.Label).Trim();
            float iconReserve = colonist.IsAbsent ? AbsentIconSize + 4f : 0f;
            float nameWidth = Mathf.Max(0f, labelCol.width - iconReserve);
            Widgets.Label(new Rect(labelCol.x, labelCol.y, nameWidth, labelCol.height), nameText.Truncate(nameWidth));

            if (colonist.IsAbsent)
            {
                Rect iconRect = new Rect(
                    labelCol.xMax - AbsentIconSize,
                    labelCol.y + (labelCol.height - AbsentIconSize) * 0.5f,
                    AbsentIconSize,
                    AbsentIconSize);
                Color prev = GUI.color;
                GUI.color = new Color(0.75f, 0.75f, 0.75f);
                GUI.DrawTexture(iconRect, TexButton.Banish);
                GUI.color = prev;
                TooltipHandler.TipRegion(iconRect, "WorkMonitor.ColonistAbsentTip".Translate());
            }
        }
    }
}
