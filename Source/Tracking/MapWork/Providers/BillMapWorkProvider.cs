using System.Collections.Generic;
using RimWorld;
using Verse;

namespace WorkMonitor.Tracking.MapWork.Providers
{
    public class BillMapWorkProvider : IMapWorkTargetProvider
    {
        public void Collect(Map map, List<ScannedMapTarget> targets)
        {
            foreach (Thing thing in map.listerThings.AllThings)
            {
                if (thing is not IBillGiver billGiver || billGiver.BillStack == null)
                {
                    continue;
                }

                foreach (Bill bill in billGiver.BillStack)
                {
                    if (!WorkLeftResolver.TryGetBillBacklog(bill, out float workLeft, out bool countable) || !countable)
                    {
                        continue;
                    }

                    List<WorkGiverDef> workGivers = MapWorkAttribution.ResolveWorkGiversForBillGiver(thing, bill);
                    if (workGivers.Count == 0)
                    {
                        continue;
                    }

                    targets.Add(new ScannedMapTarget(
                        "bill:" + bill.GetUniqueLoadID(),
                        workLeft,
                        workGivers));
                }
            }
        }
    }
}
