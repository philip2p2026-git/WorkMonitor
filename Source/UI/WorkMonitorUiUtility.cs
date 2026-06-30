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
        public const float ColonistPortraitSize = 22f;
        private const float WorkTypeAccentWidth = 3f;

        public static void DrawRowBackground(Rect row, MonitorRowKind kind, int rowIndex)
        {
            if (kind == MonitorRowKind.Total)
            {
                Widgets.DrawBoxSolid(row, new Color(1f, 1f, 1f, 0.05f));
                return;
            }

            Color kindTint = kind switch
            {
                MonitorRowKind.WorkType => new Color(0.45f, 0.58f, 0.72f, 0.07f),
                MonitorRowKind.WorkGiver => new Color(0.62f, 0.52f, 0.38f, 0.06f),
                _ => Color.clear
            };

            if (kindTint.a > 0f)
            {
                Widgets.DrawBoxSolid(row, kindTint);
            }

            if (kind == MonitorRowKind.WorkType)
            {
                Widgets.DrawBoxSolid(
                    new Rect(row.x, row.y, WorkTypeAccentWidth, row.height),
                    new Color(0.45f, 0.58f, 0.72f, 0.55f));
            }

            if (rowIndex % 2 == 1)
            {
                float stripeAlpha = kind == MonitorRowKind.WorkGiver ? 0.02f : 0.03f;
                Widgets.DrawBoxSolid(row, new Color(1f, 1f, 1f, stripeAlpha));
            }
        }

        public static void DrawColonistPortrait(Rect row, ColonistWorkStat colonist)
        {
            DrawColonistPortrait(row, colonist.PawnId, colonist.Pawn, colonist.IsAbsent);
        }

        public static void DrawColonistPortrait(Rect row, ColonistStats stats)
        {
            DrawColonistPortrait(row, stats.PawnId, stats.Pawn, stats.IsAbsent);
        }

        private static void DrawColonistPortrait(Rect row, int pawnId, Pawn pawn, bool isAbsent)
        {
            pawn ??= ColonistWorkQuery.TryResolvePawn(pawnId);
            if (pawn == null)
            {
                return;
            }

            Rect portraitRect = new Rect(
                row.x,
                row.y + (row.height - ColonistPortraitSize) * 0.5f,
                ColonistPortraitSize,
                ColonistPortraitSize);
            Color prev = GUI.color;
            if (isAbsent)
            {
                GUI.color = new Color(1f, 1f, 1f, 0.45f);
            }

            GUI.DrawTexture(
                portraitRect,
                PortraitsCache.Get(pawn, new Vector2(ColonistPortraitSize, ColonistPortraitSize), Rot4.South));
            GUI.color = prev;
        }

        public static void DrawColonistLabel(Rect labelCol, ColonistWorkStat colonist)
        {
            string passion = PassionShort(colonist.Passion);
            string nameText = (passion + " " + colonist.Label).Trim();
            float iconReserve = colonist.IsAbsent ? AbsentIconSize + 4f : 0f;
            float nameWidth = Mathf.Max(0f, labelCol.width - iconReserve);

            Color prev = GUI.color;
            if (colonist.IsAbsent)
            {
                GUI.color = new Color(0.55f, 0.55f, 0.55f);
            }

            Widgets.Label(new Rect(labelCol.x, labelCol.y, nameWidth, labelCol.height), nameText.Truncate(nameWidth));
            GUI.color = prev;

            if (colonist.IsAbsent)
            {
                Rect iconRect = new Rect(
                    labelCol.xMax - AbsentIconSize,
                    labelCol.y + (labelCol.height - AbsentIconSize) * 0.5f,
                    AbsentIconSize,
                    AbsentIconSize);
                prev = GUI.color;
                GUI.color = new Color(0.75f, 0.75f, 0.75f);
                GUI.DrawTexture(iconRect, TexButton.Banish);
                GUI.color = prev;
                TooltipHandler.TipRegion(iconRect, "WorkMonitor.ColonistAbsentTip".Translate());
            }
        }
    }
}
