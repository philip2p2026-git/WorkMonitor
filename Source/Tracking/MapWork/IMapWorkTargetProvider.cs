using System.Collections.Generic;
using Verse;

namespace WorkMonitor.Tracking.MapWork
{
    public interface IMapWorkTargetProvider
    {
        void Collect(Map map, List<ScannedMapTarget> targets);
    }
}
