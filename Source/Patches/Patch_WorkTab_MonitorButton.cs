using HarmonyLib;
using UnityEngine;
using Verse;
using WorkMonitor.Patches;
using WorkTab;

namespace WorkMonitor.Patches
{
    [HarmonyPatch(typeof(MainTabWindow_WorkTab), "DoToggleButtons")]
    public static class Patch_WorkTab_MonitorButton
    {
        private const float Margin = 6f;
        private const float ButtonWidth = 88f;
        private const float ButtonHeight = 26f;

        public static void Postfix(MainTabWindow_WorkTab __instance, Rect canvas)
        {
            Rect buttonRect = new Rect(
                canvas.xMax - ButtonWidth - Margin,
                canvas.yMax - ButtonHeight - Margin,
                ButtonWidth,
                ButtonHeight);

            if (Widgets.ButtonText(buttonRect, "WorkMonitor.OpenMonitorShort".Translate()))
            {
                WorkMonitorHistoryTab.Open();
            }

            TooltipHandler.TipRegion(buttonRect, "WorkMonitor.OpenMonitor".Translate());
        }
    }
}
