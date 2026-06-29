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
    }
}
