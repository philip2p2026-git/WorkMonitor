using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using WorkMonitor.UI;

namespace WorkMonitor.Patches
{
    [HarmonyPatch(typeof(MainTabWindow_Inspect), "DoInspectPaneButtons")]
    public static class Patch_InspectPane_ColonistWorkButton
    {
        private const float ButtonSize = 24f;
        private const float RightInset = 48f;

        public static void Postfix(Rect rect, ref float lineEndWidth)
        {
            if (Find.Selector.NumSelected != 1)
            {
                return;
            }

            Pawn pawn = Find.Selector.SingleSelectedThing as Pawn;
            if (!ColonistInspectUtility.CanOpenColonistWork(pawn))
            {
                return;
            }

            float x = rect.x + rect.width - lineEndWidth - RightInset;
            Rect buttonRect = new Rect(x, rect.y, ButtonSize, ButtonSize);
            if (Widgets.ButtonImage(buttonRect, TexButton.OpenStatsReport))
            {
                ColonistInspectUtility.OpenColonistWork(pawn);
            }

            TooltipHandler.TipRegion(buttonRect, "WorkMonitor.OpenColonistWork".Translate());
            lineEndWidth += ButtonSize;
        }
    }
}
