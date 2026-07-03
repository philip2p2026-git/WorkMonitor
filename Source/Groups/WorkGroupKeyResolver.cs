using System.Collections.Generic;
using RimWorld;
using Verse;

namespace WorkMonitor.Groups
{
    public static class WorkGroupKeyResolver
    {
        public static IEnumerable<string> ResolveGroupKeysForWorkGiver(WorkGiverDef workGiver)
        {
            if (workGiver == null)
            {
                yield break;
            }

            foreach (string key in WorkGiverAssignmentIndex.GetStorageKeysForWorkGiver(workGiver))
            {
                yield return key;
            }
        }

        public static HashSet<WorkGiverDef> GetAssignedWorkGivers()
        {
            return WorkGiverAssignmentIndex.GetAssignedWorkGivers();
        }
    }
}
