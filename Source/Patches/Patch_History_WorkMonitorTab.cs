using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using WorkMonitor.UI;

namespace WorkMonitor.Patches
{
    public static class WorkMonitorHistoryTab
    {
        public static bool Active;

        private static readonly WorkMonitorContentHost ContentHost = new WorkMonitorContentHost();

        public static void Open()
        {
            Active = true;
            MainButtonDef history = DefDatabase<MainButtonDef>.GetNamed("History");
            Find.MainTabsRoot.SetCurrentTab(history);
        }

        public static void Draw(Rect rect)
        {
            rect.yMin += 17f;
            ContentHost.Draw(rect);
        }

        public static void Deactivate()
        {
            Active = false;
            ContentHost.ResetToOverview();
        }
    }

    [HarmonyPatch(typeof(MainTabWindow_History), "PreOpen")]
    public static class Patch_History_PreOpen
    {
        private static readonly FieldInfo TabsField = AccessTools.Field(typeof(MainTabWindow_History), "tabs");
        private static readonly FieldInfo LabelField = AccessTools.Field(typeof(TabRecord), "label");
        private static readonly FieldInfo ClickedField = AccessTools.Field(typeof(TabRecord), "clickedAction");
        private static readonly FieldInfo SelectedField = AccessTools.Field(typeof(TabRecord), "selectedGetter");

        public static void Postfix(MainTabWindow_History __instance)
        {
            var tabs = (List<TabRecord>)TabsField.GetValue(__instance);
            for (int i = 0; i < tabs.Count; i++)
            {
                TabRecord tab = tabs[i];
                Action clicked = (Action)ClickedField.GetValue(tab);
                Func<bool> selected = (Func<bool>)SelectedField.GetValue(tab);
                string label = (string)LabelField.GetValue(tab);
                tabs[i] = new TabRecord(
                    label,
                    () =>
                    {
                        WorkMonitorHistoryTab.Deactivate();
                        clicked();
                    },
                    () => !WorkMonitorHistoryTab.Active && selected());
            }

            tabs.Add(new TabRecord(
                "WorkMonitor.HistoryTab".Translate(),
                () => WorkMonitorHistoryTab.Active = true,
                () => WorkMonitorHistoryTab.Active));
        }
    }

    [HarmonyPatch(typeof(MainTabWindow_History), "DoWindowContents")]
    public static class Patch_History_DoWindowContents
    {
        private static readonly FieldInfo TabsField = AccessTools.Field(typeof(MainTabWindow_History), "tabs");

        public static bool Prefix(MainTabWindow_History __instance, Rect rect)
        {
            if (!WorkMonitorHistoryTab.Active)
            {
                return true;
            }

            Rect tabRect = rect;
            tabRect.yMin += 45f;
            var tabs = (List<TabRecord>)TabsField.GetValue(__instance);
            TabDrawer.DrawTabs(tabRect, tabs);
            WorkMonitorHistoryTab.Draw(tabRect);
            return false;
        }
    }
}
