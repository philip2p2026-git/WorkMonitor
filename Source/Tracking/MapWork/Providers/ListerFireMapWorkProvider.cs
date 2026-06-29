using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace WorkMonitor.Tracking.MapWork.Providers
{
    public class ListerFireMapWorkProvider : IMapWorkTargetProvider
    {
        public void Collect(Map map, List<ScannedMapTarget> targets)
        {
            WorkGiverDef workGiver = MapWorkAttribution.GetWorkGiver("FightFires");
            if (workGiver == null)
            {
                return;
            }

            List<WorkGiverDef> workGivers = new List<WorkGiverDef> { workGiver };

            foreach (Thing thing in map.listerThings.ThingsInGroup(ThingRequestGroup.Fire))
            {
                if (thing is Fire fire)
                {
                    float work = Mathf.Max(0f, fire.fireSize * 100f);
                    targets.Add(new ScannedMapTarget(
                        "fire:" + fire.thingIDNumber,
                        work,
                        workGivers));
                }
            }
        }
    }
}
