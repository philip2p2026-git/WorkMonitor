using System.Collections.Generic;
using WorkMonitor.Tracking.MapWork.Providers;

namespace WorkMonitor.Tracking.MapWork
{
    public static class MapWorkProviderRegistry
    {
        private static readonly IMapWorkTargetProvider[] AllProviders =
        {
            new BillMapWorkProvider(),
            new FrameMapWorkProvider(),
            new FrameDeliveryMapWorkProvider(),
            new MineDesignationMapWorkProvider(),
            new UnfinishedThingMapWorkProvider(),
            new DesignationMapWorkProvider(),
            new BrokenDownBuildingMapWorkProvider(),
            new ListerFilthMapWorkProvider(),
            new ListerFireMapWorkProvider(),
            new ListerRepairMapWorkProvider(),
            new ListerHaulablesMapWorkProvider(),
            new ListerRefuelMapWorkProvider(),
            new CompBuildingMapWorkProvider(),
            new ZoneGrowingMapWorkProvider(),
            new SnowClearMapWorkProvider(),
            new SingletonResearchMapWorkProvider(),
        };

        public static IEnumerable<IMapWorkTargetProvider> All => AllProviders;
    }
}
