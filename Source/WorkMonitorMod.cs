using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using WorkMonitor.Export;
using WorkMonitor.Tracking;
using WorkMonitor.UI;

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

            listing.Label("WorkMonitor.SettingsDefaultRange".Translate());
            if (listing.ButtonText(MonitorRangeState.PresetToLabel(Settings.DefaultRangePreset)))
            {
                int idx = 0;
                for (int i = 0; i < MonitorRangeState.AllPresets.Count; i++)
                {
                    if (MonitorRangeState.AllPresets[i] == Settings.DefaultRangePreset)
                    {
                        idx = i;
                        break;
                    }
                }

                idx = (idx + 1) % MonitorRangeState.AllPresets.Count;
                Settings.defaultRangePreset = (int)MonitorRangeState.AllPresets[idx];
            }
            listing.Gap(6f);

            listing.Label("WorkMonitor.DayRolloverHour".Translate());
            if (listing.ButtonText(Settings.dayRolloverHour == 8
                ? "WorkMonitor.DayRolloverMorning".Translate()
                : "WorkMonitor.DayRolloverMidnight".Translate()))
            {
                Settings.dayRolloverHour = Settings.dayRolloverHour == 0 ? 8 : 0;
            }
            listing.Gap(6f);

            listing.Label("WorkMonitor.SettingsChartHistory".Translate());
            Settings.chartHistoryHours = (int)listing.Slider(Settings.chartHistoryHours, 6, WorkMonitorSettings.MaxRetentionHours);
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
            listing.Gap(6f);

            listing.CheckboxLabeled("WorkMonitor.SettingsShowSkillOnWorkGiver".Translate(), ref Settings.showSkillOnWorkGiverLabels);
            listing.Gap(6f);

            listing.Label("WorkMonitor.SettingsWorkGiverLabelFormat".Translate());
            Settings.workGiverLabelFormat = listing.TextEntry(Settings.workGiverLabelFormat ?? "{skill}: {label}");
            listing.Gap(6f);

            listing.Label("WorkMonitor.SettingsSkillRoleOverrides".Translate());
            Settings.skillRoleOverrides = listing.TextEntry(Settings.skillRoleOverrides ?? "");
            listing.Gap(6f);

            listing.Label("WorkMonitor.SettingsWorkGiverSkillOverrides".Translate());
            Settings.workGiverSkillOverrides = listing.TextEntry(Settings.workGiverSkillOverrides ?? "");
            listing.Gap(6f);

            listing.Label("WorkMonitor.SettingsMapSampleInterval".Translate() + ": " + MapWorkSampler.NormalizeInterval(Settings.mapSampleIntervalHours) + "h");
            if (listing.ButtonText("WorkMonitor.SettingsMapSampleCycle".Translate()))
            {
                Settings.mapSampleIntervalHours = MapWorkSampler.NormalizeInterval(Settings.mapSampleIntervalHours) switch
                {
                    1 => 2,
                    2 => 3,
                    3 => 6,
                    6 => 12,
                    _ => 1
                };
            }
            listing.Gap(12f);

            listing.Label("WorkMonitor.SettingsExport".Translate());
            if (listing.ButtonText("WorkMonitor.ExportColonistCsv".Translate()))
            {
                ExportColonistCsv();
            }

            if (listing.ButtonText("WorkMonitor.ExportMapWorkGiverCsv".Translate()))
            {
                ExportMapWorkGiverCsv();
            }

            listing.End();
        }

        private static void ExportColonistCsv()
        {
            if (WorkMonitorCsvExporter.TryExportColonistRecords(out string path, out string error))
            {
                Messages.Message("WorkMonitor.ExportSuccess".Translate(path), MessageTypeDefOf.PositiveEvent, false);
            }
            else
            {
                Messages.Message("WorkMonitor.ExportFailed".Translate(error ?? "unknown"), MessageTypeDefOf.RejectInput, false);
            }
        }

        private static void ExportMapWorkGiverCsv()
        {
            if (WorkMonitorCsvExporter.TryExportMapWorkGiverRecords(out string path, out string error))
            {
                Messages.Message("WorkMonitor.ExportSuccess".Translate(path), MessageTypeDefOf.PositiveEvent, false);
            }
            else
            {
                Messages.Message("WorkMonitor.ExportFailed".Translate(error ?? "unknown"), MessageTypeDefOf.RejectInput, false);
            }
        }

        public override string SettingsCategory()
        {
            return "WorkMonitor.ModName".Translate();
        }
    }
}
