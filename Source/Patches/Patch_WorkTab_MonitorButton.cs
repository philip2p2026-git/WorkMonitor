using HarmonyLib;
using UnityEngine;
using Verse;
using WorkMonitor.UI;
using WorkTab;

namespace WorkMonitor.Patches
{
    [HarmonyPatch(typeof(MainTabWindow_WorkTab), "DoToggleButtons")]
    public static class Patch_WorkTab_MonitorButton
    {
        private const float Margin = 6f;

        public static void Postfix(MainTabWindow_WorkTab __instance, Rect canvas)
        {
            Rect buttonRect = new Rect(canvas.xMax - 30f - Margin * 4f - 90f, canvas.yMin, 90f, 30f);
            if (Widgets.ButtonText(buttonRect, "WorkMonitor.OpenMonitor".Translate()))
            {
                WorkGroupMonitorWindow.Open();
            }
        }
    }
}
