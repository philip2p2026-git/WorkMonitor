using HarmonyLib;
using UnityEngine;
using Verse;

namespace WorkMonitor
{
    public class WorkMonitorMod : Mod
    {
        public static WorkMonitorMod Instance { get; private set; }
        public static WorkMonitorSettings Settings { get; private set; }

        public WorkMonitorMod(ModContentPack content) : base(content)
        {
            Instance = this;
            Settings = GetSettings<WorkMonitorSettings>();

            var harmony = new Harmony("philip2p2026.workmonitor");
            harmony.PatchAll();

            Log.Message("[WorkMonitor] Loaded — read-only work activity monitor.");
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);

            listing.Label("WorkMonitor.SettingsStatsWindow".Translate());
            Settings.statsWindowHours = (int)listing.Slider(Settings.statsWindowHours, 6, 48);
            listing.Gap(6f);

            listing.Label("WorkMonitor.SettingsChartHistory".Translate());
            Settings.chartHistoryHours = (int)listing.Slider(Settings.chartHistoryHours, 6, 48);
            listing.Gap(6f);

            listing.Label("WorkMonitor.SettingsGreenHours".Translate());
            Settings.greenStatusHours = (int)listing.Slider(Settings.greenStatusHours, 1, 24);
            listing.Gap(6f);

            listing.Label("WorkMonitor.SettingsYellowHours".Translate());
            Settings.yellowStatusHours = (int)listing.Slider(Settings.yellowStatusHours, 2, 48);
            listing.Gap(6f);

            listing.Label("WorkMonitor.SettingsRefreshTicks".Translate() + ": " + Settings.refreshIntervalTicks);
            Settings.refreshIntervalTicks = (int)listing.Slider(Settings.refreshIntervalTicks, 30, 300);
            listing.Gap(6f);

            listing.CheckboxLabeled("WorkMonitor.SettingsShowTimeInHours".Translate(), ref Settings.showTimeInHours);
            listing.End();
        }

        public override string SettingsCategory()
        {
            return "WorkMonitor.ModName".Translate();
        }
    }
}
