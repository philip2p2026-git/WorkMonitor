using UnityEngine;
using Verse;

namespace WorkMonitor
{
    public class WorkMonitorSettings : ModSettings
    {
        public const int TicksPerHour = 2500;

        public int statsWindowHours = 24;
        public int chartHistoryHours = 24;
        public int greenStatusHours = 6;
        public int yellowStatusHours = 12;
        public int refreshIntervalTicks = 60;
        public bool showTimeInHours = true;

        public Vector2 monitorWindowSize = new Vector2(720f, 520f);

        public int StatsWindowTicks => statsWindowHours * TicksPerHour;
        public int GreenStatusTicks => greenStatusHours * TicksPerHour;
        public int YellowStatusTicks => yellowStatusHours * TicksPerHour;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref statsWindowHours, "statsWindowHours", 24);
            Scribe_Values.Look(ref chartHistoryHours, "chartHistoryHours", 24);
            Scribe_Values.Look(ref greenStatusHours, "greenStatusHours", 6);
            Scribe_Values.Look(ref yellowStatusHours, "yellowStatusHours", 12);
            Scribe_Values.Look(ref refreshIntervalTicks, "refreshIntervalTicks", 60);
            Scribe_Values.Look(ref showTimeInHours, "showTimeInHours", true);
            Scribe_Values.Look(ref monitorWindowSize, "monitorWindowSize", new Vector2(720f, 520f));
        }
    }
}
