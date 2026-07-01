using RimWorld;
using Verse;

namespace WorkMonitor.UI
{
    public static class ColonistInspectUtility
    {
        public static void OpenPawnProfile(Pawn pawn)
        {
            if (pawn == null || pawn.Dead)
            {
                return;
            }

            Find.Selector.Select(pawn);
            InspectPaneUtility.OpenTab(typeof(ITab_Pawn_Character));
            CameraJumper.TryJumpAndSelect(pawn);
        }

        public static void OpenColonistWork(Pawn pawn)
        {
            if (pawn == null || pawn.Dead)
            {
                return;
            }

            WorkMonitor.Patches.WorkMonitorHistoryTab.OpenColonistWork(pawn);
        }

        public static bool CanOpenColonistWork(Pawn pawn)
        {
            if (pawn == null || pawn.Dead)
            {
                return false;
            }

            if (!pawn.RaceProps.Humanlike || pawn.Faction != Faction.OfPlayer)
            {
                return false;
            }

            return !pawn.DevelopmentalStage.Baby();
        }
    }
}
