using System.Collections.Generic;
using RimWorld;
using Verse;

namespace WorkMonitor.Tracking.MapWork.Providers
{
    public class UnfinishedThingMapWorkProvider : IMapWorkTargetProvider
    {
        public void Collect(Map map, List<ScannedMapTarget> targets)
        {
            foreach (Thing thing in map.listerThings.AllThings)
            {
                if (thing is not UnfinishedThing unfinished || unfinished.workLeft <= -5000f)
                {
                    continue;
                }

                float workLeft = unfinished.workLeft;
                if (workLeft <= 0f)
                {
                    continue;
                }

                List<WorkGiverDef> workGivers = MapWorkAttribution.ResolveWorkGiversForUnfinished(unfinished);
                if (workGivers.Count == 0)
                {
                    continue;
                }

                targets.Add(new ScannedMapTarget(
                    "uft:" + unfinished.thingIDNumber,
                    workLeft,
                    workGivers));
            }
        }
    }
}
