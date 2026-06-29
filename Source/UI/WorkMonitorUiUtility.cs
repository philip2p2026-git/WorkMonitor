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

        public static void DrawShareBar(Rect rect, float percent)
        {
            Widgets.DrawBoxSolid(rect, new Color(0.15f, 0.15f, 0.15f, 0.5f));
            Rect fill = rect;
            fill.width *= Mathf.Clamp01(percent / 100f);
            Widgets.DrawBoxSolid(fill, new Color(0.35f, 0.7f, 0.95f, 0.85f));
        }
    }
}
