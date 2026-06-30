using Verse;

namespace WorkMonitor.Tracking
{
    public enum ColonistPresence
    {
        Present,
        Absent
    }

    public class ColonistWorkProfile : IExposable
    {
        public int pawnId;
        public string labelShort;
        public ColonistPresence presence;

        public bool IsAbsent => presence == ColonistPresence.Absent;

        public void ExposeData()
        {
            Scribe_Values.Look(ref pawnId, "pawnId");
            Scribe_Values.Look(ref labelShort, "labelShort");
            Scribe_Values.Look(ref presence, "presence", ColonistPresence.Present);
        }
    }

    public class ColonistWorkProfileEntry : IExposable
    {
        public int pawnId;
        public ColonistWorkProfile profile = new ColonistWorkProfile();

        public void ExposeData()
        {
            Scribe_Values.Look(ref pawnId, "pawnId");
            Scribe_Deep.Look(ref profile, "profile");
            profile ??= new ColonistWorkProfile();
        }
    }
}
