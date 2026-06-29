using System.Collections.Generic;
using RimWorld;
using Verse;

namespace WorkMonitor.Tracking.MapWork.Providers
{
    public class SingletonResearchMapWorkProvider : IMapWorkTargetProvider
    {
        public void Collect(Map map, List<ScannedMapTarget> targets)
        {
            WorkGiverDef workGiver = MapWorkAttribution.GetWorkGiver("Research");
            if (workGiver == null)
            {
                return;
            }

            float remaining = MapWorkEstimate.ResearchRemaining();
            if (remaining <= 0f)
            {
                return;
            }

            targets.Add(new ScannedMapTarget(
                "research:active",
                remaining,
                new List<WorkGiverDef> { workGiver }));
        }
    }
}
