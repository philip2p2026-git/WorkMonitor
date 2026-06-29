using System.Collections.Generic;
using RimWorld;
using Verse;
using WorkMonitor.Tracking.MapWork;

namespace WorkMonitor.Tracking.MapWork.Providers
{
    public class FrameDeliveryMapWorkProvider : IMapWorkTargetProvider
    {
        public void Collect(Map map, List<ScannedMapTarget> targets)
        {
            WorkGiverDef constructDelivery = MapWorkAttribution.GetWorkGiver("ConstructDeliverResourcesToFrames");
            WorkGiverDef haulDelivery = MapWorkAttribution.GetWorkGiver("DeliverResourcesToFrames");
            if (constructDelivery == null && haulDelivery == null)
            {
                return;
            }

            foreach (Thing thing in map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingFrame))
            {
                if (thing is not Frame frame || !MapWorkFrameUtility.NeedsResourceDelivery(frame))
                {
                    continue;
                }

                List<WorkGiverDef> workGivers = new List<WorkGiverDef>();
                if (constructDelivery != null)
                {
                    workGivers.Add(constructDelivery);
                }

                if (haulDelivery != null)
                {
                    workGivers.Add(haulDelivery);
                }

                targets.Add(new ScannedMapTarget(
                    "framedelivery:" + frame.thingIDNumber,
                    0f,
                    workGivers));
            }
        }
    }
}
