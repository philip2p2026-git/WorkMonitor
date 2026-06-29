using Verse;

namespace WorkMonitor.Tracking
{
    public class WorkActivityRecord : IExposable
    {
        public int lastWorkTick;
        public int jobCount;
        public int ticksSpent;
        public float workUnitsSpent;

        public void ExposeData()
        {
            Scribe_Values.Look(ref lastWorkTick, "lastWorkTick");
            Scribe_Values.Look(ref jobCount, "jobCount");
            Scribe_Values.Look(ref ticksSpent, "ticksSpent");
            Scribe_Values.Look(ref workUnitsSpent, "workUnitsSpent", 0f);
        }
    }
}
