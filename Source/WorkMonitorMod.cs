using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using WorkMonitor.Diagnostics;
using WorkMonitor.Export;
using WorkMonitor.Tracking;
using WorkMonitor.UI;

namespace WorkMonitor
{
    public class WorkMonitorMod : Mod
    {
        private const float SettingsContentHeight = 620f;
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

            DrawRangeAndLayoutRow(listing);
            listing.Gap(SettingsControlGap);

            DrawDayRolloverSliderRow(listing);

            listing.Gap(SettingsSectionGap);
            listing.GapLine(SettingsControlGap);

            DrawSectionHeader(listing, "WorkMonitor.SettingsSectionStatus".Translate());
            DrawStatusThresholdsRow(listing);

            listing.Gap(SettingsSectionGap);
            listing.GapLine(SettingsControlGap);

            DrawSectionHeader(listing, "WorkMonitor.SettingsSectionDisplay".Translate());
            DrawDisplayCheckboxesRow(listing);
            listing.Gap(SettingsControlGap);

            DrawSkillMarkerSliderRow(listing);
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
            DrawExportButtonsRows(listing);

            listing.Gap(SettingsSectionGap);
            listing.GapLine(SettingsControlGap);

            DrawSectionHeader(listing, "WorkMonitor.SettingsSectionDiagnostics".Translate());
            DrawDiagnosticsSection(listing);
        }

        private static void DrawSectionHeader(Listing_Standard listing, string text)
        {
            listing.Gap(4f);
            listing.Label(text);
            listing.Gap(4f);
        }

        private static void DrawRangeAndLayoutRow(Listing_Standard listing)
        {
            Rect row = listing.GetRect(SettingsRowHeight);
            float gap = 8f;
            float halfWidth = (row.width - gap) * 0.5f;

            DrawRangeSliderHalf(new Rect(row.x, row.y, halfWidth, row.height));
            DrawOverviewLayoutSliderHalf(new Rect(row.x + halfWidth + gap, row.y, halfWidth, row.height));
        }

        private static void DrawRangeSliderHalf(Rect area)
        {
            const float labelWidth = 88f;
            const float valueWidth = 44f;
            float sliderWidth = area.width - labelWidth - valueWidth - SettingsControlGap;

            Widgets.Label(new Rect(area.x, area.y, labelWidth, area.height), "WorkMonitor.SettingsDefaultRange".Translate());

            int presetCount = MonitorRangeState.AllPresets.Count;
            int presetIndex = MonitorRangeState.IndexOfPreset(Settings.DefaultRangePreset);
            float sliderValue = Widgets.HorizontalSlider(
                new Rect(area.x + labelWidth, area.y + 4f, sliderWidth, area.height),
                presetIndex,
                0f,
                presetCount - 1,
                true);
            int newIndex = Mathf.RoundToInt(sliderValue);
            if (newIndex != presetIndex)
            {
                Settings.defaultRangePreset = (int)MonitorRangeState.PresetAtIndex(newIndex);
            }

            DrawRightLabel(
                new Rect(area.xMax - valueWidth, area.y, valueWidth, area.height),
                MonitorRangeState.PresetToLabel(Settings.DefaultRangePreset));
        }

        private static void DrawOverviewLayoutSliderHalf(Rect area)
        {
            const float labelWidth = 88f;
            const float valueWidth = 88f;
            float sliderWidth = area.width - labelWidth - valueWidth - SettingsControlGap;

            Widgets.Label(new Rect(area.x, area.y, labelWidth, area.height), "WorkMonitor.SettingsDefaultOverviewLayout".Translate());

            int layoutCount = 3;
            int layoutIndex = Mathf.Clamp((int)Settings.overviewLayoutMode, 0, layoutCount - 1);
            float sliderValue = Widgets.HorizontalSlider(
                new Rect(area.x + labelWidth, area.y + 4f, sliderWidth, area.height),
                layoutIndex,
                0f,
                layoutCount - 1,
                true);
            int newIndex = Mathf.RoundToInt(sliderValue);
            if (newIndex != layoutIndex)
            {
                Settings.overviewLayoutMode = (OverviewLayoutMode)newIndex;
            }

            DrawRightLabel(
                new Rect(area.xMax - valueWidth, area.y, valueWidth, area.height),
                OverviewLayoutModeLabel(Settings.overviewLayoutMode));
        }

        private static string OverviewLayoutModeLabel(OverviewLayoutMode mode)
        {
            return mode switch
            {
                OverviewLayoutMode.WorkTypeWorkGiverFirst => "WorkMonitor.GroupByWorkGiver".Translate(),
                OverviewLayoutMode.ColonistTopLevel => "WorkMonitor.GroupByColonistTop".Translate(),
                _ => "WorkMonitor.GroupByColonist".Translate()
            };
        }

        private static void DrawDayRolloverSliderRow(Listing_Standard listing)
        {
            Rect row = listing.GetRect(SettingsRowHeight);
            float labelWidth = 132f;
            float valueWidth = 48f;
            float sliderWidth = row.width - labelWidth - valueWidth - SettingsControlGap;

            Widgets.Label(new Rect(row.x, row.y, labelWidth, row.height), "WorkMonitor.SettingsDayRollover".Translate());

            int rolloverIndex = WorkMonitorSettings.IndexOfDayRolloverHour(Settings.dayRolloverHour);
            int rolloverCount = WorkMonitorSettings.DayRolloverHourOptions.Length;
            float sliderValue = Widgets.HorizontalSlider(
                new Rect(row.x + labelWidth, row.y + 4f, sliderWidth, row.height),
                rolloverIndex,
                0f,
                rolloverCount - 1,
                true);
            int newIndex = Mathf.RoundToInt(sliderValue);
            if (newIndex != rolloverIndex)
            {
                Settings.dayRolloverHour = WorkMonitorSettings.DayRolloverHourAtIndex(newIndex);
            }

            DrawRightLabel(
                new Rect(row.xMax - valueWidth, row.y, valueWidth, row.height),
                WorkMonitorSettings.FormatDayRolloverHour(Settings.dayRolloverHour));
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

            bool showTimeInHours = Settings.showTimeInHours;
            Widgets.CheckboxLabeled(
                new Rect(row.x, row.y, row.width, row.height),
                "WorkMonitor.SettingsShowTimeInHours".Translate(),
                ref showTimeInHours);
            Settings.showTimeInHours = showTimeInHours;
        }

        private static void DrawSkillMarkerSliderRow(Listing_Standard listing)
        {
            Rect row = listing.GetRect(SettingsRowHeight);
            float labelWidth = 132f;
            float valueWidth = 72f;
            float sliderWidth = row.width - labelWidth - valueWidth - SettingsControlGap;

            Widgets.Label(new Rect(row.x, row.y, labelWidth, row.height), "WorkMonitor.SettingsSkillMarker".Translate());

            int markerIndex = (int)Settings.skillMarkerMode;
            float sliderValue = Widgets.HorizontalSlider(
                new Rect(row.x + labelWidth, row.y + 4f, sliderWidth, row.height),
                markerIndex,
                0f,
                2f,
                true);
            int newIndex = Mathf.Clamp(Mathf.RoundToInt(sliderValue), 0, 2);
            if (newIndex != markerIndex)
            {
                Settings.skillMarkerMode = (WorkGiverSkillMarkerMode)newIndex;
            }

            string valueText = Settings.skillMarkerMode switch
            {
                WorkGiverSkillMarkerMode.Parentheses => "WorkMonitor.SkillMarkerParentheses".Translate(),
                WorkGiverSkillMarkerMode.Asterisk => "WorkMonitor.SkillMarkerAsterisk".Translate(),
                _ => "WorkMonitor.SkillMarkerOff".Translate()
            };
            DrawRightLabel(new Rect(row.xMax - valueWidth, row.y, valueWidth, row.height), valueText);
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

        private static void DrawDiagnosticsSection(Listing_Standard listing)
        {
            Rect checkboxRow = listing.GetRect(SettingsRowHeight);
            bool enablePerf = Settings.enablePerfLogging;
            Widgets.CheckboxLabeled(
                new Rect(checkboxRow.x, checkboxRow.y, checkboxRow.width, checkboxRow.height),
                "WorkMonitor.SettingsEnablePerfLogging".Translate(),
                ref enablePerf);
            if (enablePerf != Settings.enablePerfLogging)
            {
                Settings.enablePerfLogging = enablePerf;
                WorkMonitorPerfRecorder.OnSettingsToggled(enablePerf);
            }

            listing.Gap(SettingsControlGap);
            DrawPerfFlushSliderRow(listing);

            listing.Gap(SettingsControlGap);
            Rect buttonRow = listing.GetRect(SettingsRowHeight);
            float gap = SettingsControlGap;
            float buttonWidth = (buttonRow.width - gap * 2f) / 3f;

            if (Widgets.ButtonText(new Rect(buttonRow.x, buttonRow.y, buttonWidth, buttonRow.height), "WorkMonitor.ExportPerfLog".Translate()))
            {
                ExportPerfLog(openFolder: false);
            }

            if (Widgets.ButtonText(new Rect(buttonRow.x + buttonWidth + gap, buttonRow.y, buttonWidth, buttonRow.height), "WorkMonitor.OpenPerfFolder".Translate()))
            {
                ExportPerfLog(openFolder: true);
            }

            if (Widgets.ButtonText(new Rect(buttonRow.x + (buttonWidth + gap) * 2f, buttonRow.y, buttonWidth, buttonRow.height), "WorkMonitor.ResetPerfSession".Translate()))
            {
                WorkMonitorPerfRecorder.ResetSession();
                Messages.Message("WorkMonitor.ResetPerfSessionDone".Translate(), MessageTypeDefOf.NeutralEvent, false);
            }
        }

        private static void DrawPerfFlushSliderRow(Listing_Standard listing)
        {
            Rect row = listing.GetRect(SettingsRowHeight);
            float labelWidth = 148f;
            float valueWidth = 36f;
            float sliderWidth = row.width - labelWidth - valueWidth - SettingsControlGap;

            Widgets.Label(new Rect(row.x, row.y, labelWidth, row.height), "WorkMonitor.SettingsPerfFlushHours".Translate());

            float sliderValue = Widgets.HorizontalSlider(
                new Rect(row.x + labelWidth, row.y + 4f, sliderWidth, row.height),
                Settings.perfLogFlushHours,
                1f,
                12f,
                true);
            int newValue = Mathf.Clamp(Mathf.RoundToInt(sliderValue), 1, 12);
            if (newValue != Settings.perfLogFlushHours)
            {
                Settings.perfLogFlushHours = newValue;
            }

            DrawRightLabel(
                new Rect(row.xMax - valueWidth, row.y, valueWidth, row.height),
                Settings.perfLogFlushHours + "h");
        }

        private static void ExportPerfLog(bool openFolder)
        {
            if (WorkMonitorPerfRecorder.TryExportNow(out string path, out string error))
            {
                Messages.Message("WorkMonitor.ExportSuccess".Translate(path), MessageTypeDefOf.PositiveEvent, false);
                if (openFolder)
                {
                    WorkMonitorPerfRecorder.OpenPerfDirectory();
                }
            }
            else
            {
                Messages.Message("WorkMonitor.ExportFailed".Translate(error ?? "unknown"), MessageTypeDefOf.RejectInput, false);
            }
        }

        private static void DrawExportButtonsRows(Listing_Standard listing)
        {
            Rect row = listing.GetRect(SettingsRowHeight);
            float gap = SettingsControlGap;
            float buttonWidth = (row.width - gap * 3f) / 4f;

            if (Widgets.ButtonText(new Rect(row.x, row.y, buttonWidth, row.height), "WorkMonitor.ExportColonistCsv".Translate()))
            {
                ExportColonistCsv();
            }

            if (Widgets.ButtonText(new Rect(row.x + buttonWidth + gap, row.y, buttonWidth, row.height), "WorkMonitor.ExportMapWorkGiverCsv".Translate()))
            {
                ExportMapWorkGiverCsv();
            }

            if (Widgets.ButtonText(new Rect(row.x + (buttonWidth + gap) * 2f, row.y, buttonWidth, row.height), "WorkMonitor.ExportBoth".Translate()))
            {
                ExportBothCsv();
            }

            if (Widgets.ButtonText(new Rect(row.x + (buttonWidth + gap) * 3f, row.y, buttonWidth, row.height), "WorkMonitor.OpenExportFolder".Translate()))
            {
                WorkMonitorCsvExporter.OpenExportDirectory();
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

        private static void ExportBothCsv()
        {
            if (WorkMonitorCsvExporter.TryExportBoth(out string colonistPath, out string mapPath, out string error))
            {
                Messages.Message("WorkMonitor.ExportBothSuccess".Translate(colonistPath, mapPath), MessageTypeDefOf.PositiveEvent, false);
                return;
            }

            if (!colonistPath.NullOrEmpty() || !mapPath.NullOrEmpty())
            {
                string path = !colonistPath.NullOrEmpty() ? colonistPath : mapPath;
                Messages.Message("WorkMonitor.ExportPartialSuccess".Translate(path, error ?? "unknown"), MessageTypeDefOf.RejectInput, false);
                return;
            }

            Messages.Message("WorkMonitor.ExportFailed".Translate(error ?? "unknown"), MessageTypeDefOf.RejectInput, false);
        }

        public override string SettingsCategory()
        {
            return "WorkMonitor.ModName".Translate();
        }
    }
}
