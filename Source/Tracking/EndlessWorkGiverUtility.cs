using System.Collections.Generic;
using RimWorld;
using Verse;

namespace WorkMonitor.Tracking
{
    public static class EndlessWorkGiverUtility
    {
        private static readonly HashSet<string> EndlessDefNames = new HashSet<string>
        {
            "Research",
            "Drill",
            "GroundPenetratingScan",
            "OperateScanner"
        };

        public static bool IsEndless(WorkGiverDef workGiver)
        {
            if (workGiver == null || workGiver.defName.NullOrEmpty())
            {
                return false;
            }

            return EndlessDefNames.Contains(workGiver.defName);
        }
    }
}
