using System.Collections;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace WorkMonitor.Tracking.MapWork
{
    public static class MapWorkFrameUtility
    {
        private static readonly MethodInfo MaterialsNeededMethod =
            AccessTools.Method(typeof(Frame), "MaterialsNeeded");

        public static bool NeedsResourceDelivery(Frame frame)
        {
            if (frame == null || !frame.Spawned)
            {
                return false;
            }

            if (MaterialsNeededMethod != null)
            {
                if (MaterialsNeededMethod.Invoke(frame, null) is IList list)
                {
                    return list.Count > 0;
                }
            }

            return frame.WorkLeft > 0f;
        }
    }
}
