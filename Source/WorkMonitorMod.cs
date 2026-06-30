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
        private const float SettingsContentHeight = 580f;
        private const float SettingsRowHeight = 28f;
        private const float SettingsSectionGap = 10f;
        private const float SettingsControlGap = 6f;

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
            DrawSettingsContents(listing);
            listing.End();

            Widgets.EndScrollView();
        }

        private static void DrawSettingsContents(Listing_Standard listing)
        {
            DrawSectionHeader(listing, "WorkMonitor.SettingsSectionRange".Translate());

            DrawPresetSliderRow(listing);
            listing.Gap(SettingsControlGap);

            bool dayRolloverMorning = Settings.dayRolloverHour == 8;
            listing.CheckboxLabeled("WorkMonitor.SettingsDayRolloverAtMorning".Translate(), ref dayRolloverMorning);
            Settings.dayRolloverHour = dayRolloverMorning ? 8 : 0;

            listing.Gap(SettingsSectionGap);
            listing.GapLine(SettingsControlGap);

            DrawSectionHeader(listing, "WorkMonitor.SettingsSectionStatus".Translate());
            DrawStatusThresholdsRow(listing);

            listing.Gap(SettingsSectionGap);
            listing.GapLine(SettingsControlGap);

            DrawSectionHeader(listing, "WorkMonitor.SettingsSectionDisplay".Translate());
            DrawDisplayCheckboxesRow(listing);
            listing.Gap(SettingsControlGap);

            listing.Label("WorkMonitor.SettingsWorkGiverLabelFormat".Translate());
            Settings.workGiverLabelFormat = listing.TextEntry(Settings.workGiverLabelFormat ?? "{skill}: {label}");
            listing.Gap(SettingsControlGap);

            listing.Label("WorkMonitor.SettingsSkillRoleOverrides".Translate());
            Settings.skillRoleOverrides = listing.TextEntry(Settings.skillRoleOverrides ?? "");
            listing.Gap(SettingsControlGap);

            listing.Label("WorkMonitor.SettingsWorkGiverSkillOverrides".Translate());
            Settings.workGiverSkillOverrides = listing.TextEntry(Settings.workGiverSkillOverrides ?? "");

            listing.Gap(SettingsSectionGap);
            listing.GapLine(SettingsControlGap);

            DrawSectionHeader(listing, "WorkMonitor.SettingsSectionMap".Translate());
            DrawMapSampleSliderRow(listing);

            listing.Gap(SettingsSectionGap);
            listing.GapLine(SettingsControlGap);

            DrawSectionHeader(listing, "WorkMonitor.SettingsExport".Translate());
            listing.Gap(4f);
            DrawExportButtonsRow(listing);
        }

        private static void DrawSectionHeader(Listing_Standard listing, string text)
        {
            listing.Gap(4f);
            listing.Label(text);
            listing.Gap(4f);
        }

        private static void DrawPresetSliderRow(Listing_Standard listing)
        {
            Rect row = listing.GetRect(SettingsRowHeight);
            float labelWidth = 132f;
            float valueWidth = 76f;
            float sliderWidth = row.width - labelWidth - valueWidth - SettingsControlGap;

            Widgets.Label(new Rect(row.x, row.y, labelWidth, row.height), "WorkMonitor.SettingsDefaultRange".Translate());

            int presetCount = MonitorRangeState.AllPresets.Count;
            int presetIndex = MonitorRangeState.IndexOfPreset(Settings.DefaultRangePreset);
            float sliderValue = Widgets.HorizontalSlider(
                new Rect(row.x + labelWidth, row.y + 4f, sliderWidth, row.height),
                presetIndex,
                0f,
                presetCount - 1,
                true);
            int newIndex = Mathf.RoundToInt(sliderValue);
            if (newIndex != presetIndex)
            {
                Settings.defaultRangePreset = (int)MonitorRangeState.PresetAtIndex(newIndex);
            }

            DrawRightLabel(new Rect(row.xMax - valueWidth, row.y, valueWidth, row.height), MonitorRangeState.PresetToLabel(Settings.DefaultRangePreset));
        }

        private static void DrawStatusThresholdsRow(Listing_Standard listing)
        {
            Rect row = listing.GetRect(SettingsRowHeight);
            float gap = 8f;
            float columnWidth = (row.width - gap * 2f) / 3f;

            DrawThresholdSlider(
                new Rect(row.x, row.y, columnWidth, row.height),
                "WorkMonitor.SettingsGreenShort".Translate(),
                ref Settings.greenStatusHours,
                1,
                24,
                "h");

            DrawThresholdSlider(
                new Rect(row.x + columnWidth + gap, row.y, columnWidth, row.height),
                "WorkMonitor.SettingsYellowShort".Translate(),
                ref Settings.yellowStatusHours,
                2,
                48,
                "h");

            DrawThresholdSlider(
                new Rect(row.x + (columnWidth + gap) * 2f, row.y, columnWidth, row.height),
                "WorkMonitor.SettingsRefreshShort".Translate(),
                ref Settings.refreshIntervalTicks,
                30,
                300,
                "");
        }

        private static void DrawThresholdSlider(Rect area, string label, ref int value, int min, int max, string suffix)
        {
            const float labelWidth = 52f;
            const float valueWidth = 28f;
            float sliderWidth = Mathf.Max(24f, area.width - labelWidth - valueWidth - 2f);

            Widgets.Label(new Rect(area.x, area.y, labelWidth, area.height), label);
            float sliderValue = Widgets.HorizontalSlider(
                new Rect(area.x + labelWidth, area.y + 4f, sliderWidth, area.height),
                value,
                min,
                max,
                true);
            int newValue = Mathf.RoundToInt(sliderValue);
            if (newValue != value)
            {
                value = newValue;
            }

            string valueText = suffix.NullOrEmpty() ? value.ToString() : value + suffix;
            DrawRightLabel(new Rect(area.xMax - valueWidth, area.y, valueWidth, area.height), valueText);
        }

        private static void DrawDisplayCheckboxesRow(Listing_Standard listing)
        {
            Rect row = listing.GetRect(SettingsRowHeight);
            float gap = 8f;
            float columnWidth = (row.width - gap) * 0.5f;

            bool showTimeInHours = Settings.showTimeInHours;
            Widgets.CheckboxLabeled(
                new Rect(row.x, row.y, columnWidth, row.height),
                "WorkMonitor.SettingsShowTimeInHours".Translate(),
                ref showTimeInHours);
            Settings.showTimeInHours = showTimeInHours;

            bool showSkillOnWorkGiver = Settings.showSkillOnWorkGiverLabels;
            Widgets.CheckboxLabeled(
                new Rect(row.x + columnWidth + gap, row.y, columnWidth, row.height),
                "WorkMonitor.SettingsShowSkillOnWorkGiver".Translate(),
                ref showSkillOnWorkGiver);
            Settings.showSkillOnWorkGiverLabels = showSkillOnWorkGiver;
        }

        private static void DrawMapSampleSliderRow(Listing_Standard listing)
        {
            Rect row = listing.GetRect(SettingsRowHeight);
            float labelWidth = 148f;
            float valueWidth = 36f;
            float sliderWidth = row.width - labelWidth - valueWidth - SettingsControlGap;

            int intervalIndex = MapWorkSampler.IndexOfInterval(Settings.mapSampleIntervalHours);
            int intervalCount = MapWorkSampler.MapSampleIntervalOptions.Length;

            Widgets.Label(new Rect(row.x, row.y, labelWidth, row.height), "WorkMonitor.SettingsMapSampleInterval".Translate());

            float sliderValue = Widgets.HorizontalSlider(
                new Rect(row.x + labelWidth, row.y + 4f, sliderWidth, row.height),
                intervalIndex,
                0f,
                intervalCount - 1,
                true);
            int newIndex = Mathf.RoundToInt(sliderValue);
            if (newIndex != intervalIndex)
            {
                Settings.mapSampleIntervalHours = MapWorkSampler.IntervalAtIndex(newIndex);
            }

            DrawRightLabel(
                new Rect(row.xMax - valueWidth, row.y, valueWidth, row.height),
                MapWorkSampler.NormalizeInterval(Settings.mapSampleIntervalHours) + "h");
        }

        private static void DrawExportButtonsRow(Listing_Standard listing)
        {
            Rect row = listing.GetRect(SettingsRowHeight);
            float gap = SettingsControlGap;
            float buttonWidth = (row.width - gap) * 0.5f;

            if (Widgets.ButtonText(new Rect(row.x, row.y, buttonWidth, row.height), "WorkMonitor.ExportColonistCsv".Translate()))
            {
                ExportColonistCsv();
            }

            if (Widgets.ButtonText(new Rect(row.x + buttonWidth + gap, row.y, buttonWidth, row.height), "WorkMonitor.ExportMapWorkGiverCsv".Translate()))
            {
                ExportMapWorkGiverCsv();
            }
        }

        private static void DrawRightLabel(Rect rect, string text)
        {
            Text.Anchor = TextAnchor.MiddleRight;
            Widgets.Label(rect, text);
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
