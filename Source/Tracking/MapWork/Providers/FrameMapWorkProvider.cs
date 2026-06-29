using System.Collections.Generic;
using RimWorld;
using Verse;

namespace WorkMonitor.Tracking.MapWork.Providers
{
    public class FrameMapWorkProvider : IMapWorkTargetProvider
    {
        public void Collect(Map map, List<ScannedMapTarget> targets)
        {
            WorkGiverDef workGiver = MapWorkAttribution.ConstructFinishFramesWorkGiver();
            if (workGiver == null)
            {
                return;
            }

            List<WorkGiverDef> workGivers = new List<WorkGiverDef> { workGiver };

            foreach (Thing thing in map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingFrame))
            {
                if (thing is not Frame frame || frame.WorkLeft <= 0f)
                {
                    continue;
                }

                targets.Add(new ScannedMapTarget(
                    "frame:" + frame.thingIDNumber,
                    MapWorkEstimate.FromFrame(frame),
                    workGivers));
            }
        }
    }
}
