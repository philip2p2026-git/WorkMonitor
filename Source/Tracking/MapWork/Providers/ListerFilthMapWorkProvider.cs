using System.Collections.Generic;
using RimWorld;
using Verse;

namespace WorkMonitor.Tracking.MapWork.Providers
{
    public class ListerFilthMapWorkProvider : IMapWorkTargetProvider
    {
        public void Collect(Map map, List<ScannedMapTarget> targets)
        {
            WorkGiverDef workGiver = MapWorkAttribution.GetWorkGiver("CleanFilth");
            if (workGiver == null)
            {
                return;
            }

            List<WorkGiverDef> workGivers = new List<WorkGiverDef> { workGiver };
            HashSet<int> seen = new HashSet<int>();

            foreach (Filth filth in map.listerFilthInHomeArea.FilthInHomeArea)
            {
                if (filth == null || !filth.Spawned || !seen.Add(filth.thingIDNumber))
                {
                    continue;
                }

                targets.Add(new ScannedMapTarget(
                    "filth:" + filth.thingIDNumber,
                    MapWorkEstimate.FromFilth(filth),
                    workGivers));
            }
        }
    }
}
