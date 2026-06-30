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
        private const float SettingsContentHeight = 650f;
        private const float SettingsRowHeight = 28f;

        private Vector2 settingsScrollPosition;

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
            float contentWidth = inRect.width - 20f;
            Rect viewRect = new Rect(0f, 0f, contentWidth, SettingsContentHeight);

            Widgets.BeginScrollView(inRect, ref settingsScrollPosition, viewRect);

            Listing_Standard listing = new Listing_Standard();
            listing.Begin(viewRect);
            DrawSettingsContents(listing, contentWidth);
            listing.End();

            Widgets.EndScrollView();
        }

        private static void DrawSettingsContents(Listing_Standard listing, float contentWidth)
        {
            listing.Label("WorkMonitor.SettingsSectionRange".Translate());
            listing.Gap(4f);

            DrawPresetSliderRow(listing, contentWidth);
            listing.Gap(6f);

            bool dayRolloverMorning = Settings.dayRolloverHour == 8;
            listing.CheckboxLabeled("WorkMonitor.SettingsDayRolloverAtMorning".Translate(), ref dayRolloverMorning);
            Settings.dayRolloverHour = dayRolloverMorning ? 8 : 0;
            listing.Gap(6f);

            listing.Label("WorkMonitor.SettingsChartHistory".Translate() + ": " + Settings.chartHistoryHours + "h");
            Settings.chartHistoryHours = (int)listing.Slider(Settings.chartHistoryHours, 6, WorkMonitorSettings.MaxRetentionHours);
            listing.Gap(10f);
            listing.GapLine(6f);

            listing.Label("WorkMonitor.SettingsSectionStatus".Translate());
            listing.Gap(4f);

            listing.Label("WorkMonitor.SettingsGreenHours".Translate() + ": " + Settings.greenStatusHours + "h");
            Settings.greenStatusHours = (int)listing.Slider(Settings.greenStatusHours, 1, 24);
            listing.Gap(6f);

            listing.Label("WorkMonitor.SettingsYellowHours".Translate() + ": " + Settings.yellowStatusHours + "h");
            Settings.yellowStatusHours = (int)listing.Slider(Settings.yellowStatusHours, 2, 48);
            listing.Gap(6f);

            listing.Label("WorkMonitor.SettingsRefreshTicks".Translate() + ": " + Settings.refreshIntervalTicks);
            Settings.refreshIntervalTicks = (int)listing.Slider(Settings.refreshIntervalTicks, 30, 300);
            listing.Gap(10f);
            listing.GapLine(6f);

            listing.Label("WorkMonitor.SettingsSectionDisplay".Translate());
            listing.Gap(4f);

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
            listing.Gap(10f);
            listing.GapLine(6f);

            listing.Label("WorkMonitor.SettingsSectionMap".Translate());
            listing.Gap(4f);

            DrawMapSampleSliderRow(listing, contentWidth);
            listing.Gap(10f);
            listing.GapLine(6f);

            listing.Label("WorkMonitor.SettingsExport".Translate());
            listing.Gap(4f);

            Rect exportRow = listing.GetRect(SettingsRowHeight);
            float exportGap = 6f;
            float exportButtonWidth = (exportRow.width - exportGap) * 0.5f;
            if (Widgets.ButtonText(new Rect(exportRow.x, exportRow.y, exportButtonWidth, exportRow.height), "WorkMonitor.ExportColonistCsv".Translate()))
            {
                ExportColonistCsv();
            }

            if (Widgets.ButtonText(new Rect(exportRow.x + exportButtonWidth + exportGap, exportRow.y, exportButtonWidth, exportRow.height), "WorkMonitor.ExportMapWorkGiverCsv".Translate()))
            {
                ExportMapWorkGiverCsv();
            }
        }

        private static void DrawPresetSliderRow(Listing_Standard listing, float contentWidth)
        {
            Rect row = listing.GetRect(SettingsRowHeight);
            float labelWidth = 140f;
            float valueWidth = 72f;
            float sliderX = row.x + labelWidth;
            float sliderWidth = row.width - labelWidth - valueWidth - 6f;

            Widgets.Label(new Rect(row.x, row.y, labelWidth, row.height), "WorkMonitor.SettingsDefaultRange".Translate());

            int presetCount = MonitorRangeState.AllPresets.Count;
            int presetIndex = MonitorRangeState.IndexOfPreset(Settings.DefaultRangePreset);
            float sliderValue = Widgets.HorizontalSlider(
                new Rect(sliderX, row.y + 4f, sliderWidth, row.height),
                presetIndex,
                0f,
                presetCount - 1,
                true);
            int newIndex = Mathf.RoundToInt(sliderValue);
            if (newIndex != presetIndex)
            {
                Settings.defaultRangePreset = (int)MonitorRangeState.PresetAtIndex(newIndex);
            }

            Text.Anchor = TextAnchor.MiddleRight;
            Widgets.Label(
                new Rect(row.xMax - valueWidth, row.y, valueWidth, row.height),
                MonitorRangeState.PresetToLabel(Settings.DefaultRangePreset));
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private static void DrawMapSampleSliderRow(Listing_Standard listing, float contentWidth)
        {
            Rect row = listing.GetRect(SettingsRowHeight);
            float labelWidth = 160f;
            float valueWidth = 36f;
            float sliderX = row.x + labelWidth;
            float sliderWidth = row.width - labelWidth - valueWidth - 6f;

            int intervalIndex = MapWorkSampler.IndexOfInterval(Settings.mapSampleIntervalHours);
            int intervalCount = MapWorkSampler.MapSampleIntervalOptions.Length;

            Widgets.Label(new Rect(row.x, row.y, labelWidth, row.height), "WorkMonitor.SettingsMapSampleInterval".Translate());

            float sliderValue = Widgets.HorizontalSlider(
                new Rect(sliderX, row.y + 4f, sliderWidth, row.height),
                intervalIndex,
                0f,
                intervalCount - 1,
                true);
            int newIndex = Mathf.RoundToInt(sliderValue);
            if (newIndex != intervalIndex)
            {
                Settings.mapSampleIntervalHours = MapWorkSampler.IntervalAtIndex(newIndex);
            }

            Text.Anchor = TextAnchor.MiddleRight;
            Widgets.Label(
                new Rect(row.xMax - valueWidth, row.y, valueWidth, row.height),
                MapWorkSampler.NormalizeInterval(Settings.mapSampleIntervalHours) + "h");
            Text.Anchor = TextAnchor.UpperLeft;
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
