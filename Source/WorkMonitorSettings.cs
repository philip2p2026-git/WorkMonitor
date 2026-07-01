using System.Collections.Generic;
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
        public UI.OverviewLayoutMode overviewLayoutMode = UI.OverviewLayoutMode.WorkTypeColonistFirst;
        public UI.WorkGiverSkillMarkerMode skillMarkerMode = UI.WorkGiverSkillMarkerMode.Parentheses;

        public bool WorkGiverFirst => overviewLayoutMode == UI.OverviewLayoutMode.WorkTypeWorkGiverFirst;

        public bool ColonistTopLevel => overviewLayoutMode == UI.OverviewLayoutMode.ColonistTopLevel;
        public string workGiverSkillOverrides = "";
        public int mapSampleIntervalHours = 1;
        public int defaultRangePreset = (int)UI.MonitorRangePreset.Hours24;
        public int dayRolloverHour = 5;

        public static readonly int[] DayRolloverHourOptions = { 0, 5, 8 };
        public int maxDailyBuckets = 20;
        public int maxQuadrumBuckets = 12;
        public int maxYearBuckets = 7;
        public bool yearHistoryUnlimited = false;

        public UI.MonitorRangePreset DefaultRangePreset => (UI.MonitorRangePreset)defaultRangePreset;

        public Vector2 monitorWindowSize = new Vector2(720f, 520f);

        private Dictionary<string, bool> workGiverSkillOverrideCache;

        public int StatsWindowTicks => statsWindowHours * TicksPerHour;
        public int GreenStatusTicks => greenStatusHours * TicksPerHour;
        public int YellowStatusTicks => yellowStatusHours * TicksPerHour;

        public const int MaxRetentionHours = 72;

        public int ResolveRetentionHours(int activeRangeHours = -1)
        {
            int range = activeRangeHours > 0 ? activeRangeHours : statsWindowHours;
            return UnityEngine.Mathf.Clamp(UnityEngine.Mathf.Min(range, MaxRetentionHours), 6, MaxRetentionHours);
        }

        public static int NormalizeDayRolloverHour(int hour)
        {
            foreach (int option in DayRolloverHourOptions)
            {
                if (hour == option)
                {
                    return option;
                }
            }

            int best = DayRolloverHourOptions[1];
            int bestDistance = int.MaxValue;
            foreach (int option in DayRolloverHourOptions)
            {
                int distance = UnityEngine.Mathf.Abs(hour - option);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = option;
                }
            }

            return best;
        }

        public static int IndexOfDayRolloverHour(int hour)
        {
            int normalized = NormalizeDayRolloverHour(hour);
            for (int i = 0; i < DayRolloverHourOptions.Length; i++)
            {
                if (DayRolloverHourOptions[i] == normalized)
                {
                    return i;
                }
            }

            return 1;
        }

        public static int DayRolloverHourAtIndex(int index)
        {
            return DayRolloverHourOptions[UnityEngine.Mathf.Clamp(index, 0, DayRolloverHourOptions.Length - 1)];
        }

        public static string FormatDayRolloverHour(int hour)
        {
            return NormalizeDayRolloverHour(hour).ToString("00") + ":00";
        }

        public bool TryGetWorkGiverSkillOverride(string workGiverDefName, out bool usesSkill)
        {
            usesSkill = false;
            if (workGiverDefName.NullOrEmpty())
            {
                return false;
            }

            EnsureWorkGiverSkillOverrideCache();
            return workGiverSkillOverrideCache.TryGetValue(workGiverDefName, out usesSkill);
        }

        private void EnsureWorkGiverSkillOverrideCache()
        {
            if (workGiverSkillOverrideCache != null)
            {
                return;
            }

            workGiverSkillOverrideCache = new Dictionary<string, bool>();
            if (workGiverSkillOverrides.NullOrEmpty())
            {
                return;
            }

            string[] pairs = workGiverSkillOverrides.Split(',');
            foreach (string pair in pairs)
            {
                string trimmed = pair.Trim();
                if (trimmed.NullOrEmpty())
                {
                    continue;
                }

                int eq = trimmed.IndexOf('=');
                if (eq <= 0)
                {
                    continue;
                }

                string key = trimmed.Substring(0, eq).Trim();
                string value = trimmed.Substring(eq + 1).Trim();
                if (key.NullOrEmpty() || value.NullOrEmpty())
                {
                    continue;
                }

                if (bool.TryParse(value, out bool parsed))
                {
                    workGiverSkillOverrideCache[key] = parsed;
                }
            }
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref statsWindowHours, "statsWindowHours", 24);
            Scribe_Values.Look(ref chartHistoryHours, "chartHistoryHours", 24);
            Scribe_Values.Look(ref greenStatusHours, "greenStatusHours", 6);
            Scribe_Values.Look(ref yellowStatusHours, "yellowStatusHours", 12);
            Scribe_Values.Look(ref refreshIntervalTicks, "refreshIntervalTicks", 60);
            Scribe_Values.Look(ref showTimeInHours, "showTimeInHours", true);

            int layoutModeInt = (int)overviewLayoutMode;
            Scribe_Values.Look(ref layoutModeInt, "overviewLayoutMode", -1);
            bool legacyGroupDetailWorkGiverFirst = false;
            Scribe_Values.Look(ref legacyGroupDetailWorkGiverFirst, "groupDetailWorkGiverFirst", false);
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                if (layoutModeInt < 0)
                {
                    overviewLayoutMode = legacyGroupDetailWorkGiverFirst
                        ? UI.OverviewLayoutMode.WorkTypeWorkGiverFirst
                        : UI.OverviewLayoutMode.WorkTypeColonistFirst;
                }
                else
                {
                    overviewLayoutMode = (UI.OverviewLayoutMode)layoutModeInt;
                }
            }

            int skillMarkerModeInt = (int)skillMarkerMode;
            Scribe_Values.Look(ref skillMarkerModeInt, "skillMarkerMode", -1);

            bool legacyShowSkillOnWorkGiverLabels = true;
            Scribe_Values.Look(ref legacyShowSkillOnWorkGiverLabels, "showSkillOnWorkGiverLabels", true);
            string legacyWorkGiverLabelFormat = null;
            Scribe_Values.Look(ref legacyWorkGiverLabelFormat, "workGiverLabelFormat", "{skill} {label}");
            string legacySkillRoleOverrides = null;
            Scribe_Values.Look(ref legacySkillRoleOverrides, "skillRoleOverrides", "");

            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                if (skillMarkerModeInt < 0)
                {
                    skillMarkerMode = legacyShowSkillOnWorkGiverLabels
                        ? UI.WorkGiverSkillMarkerMode.Parentheses
                        : UI.WorkGiverSkillMarkerMode.Off;
                }
                else
                {
                    skillMarkerMode = (UI.WorkGiverSkillMarkerMode)Mathf.Clamp(skillMarkerModeInt, 0, 2);
                }
            }

            Scribe_Values.Look(ref workGiverSkillOverrides, "workGiverSkillOverrides", "");
            Scribe_Values.Look(ref mapSampleIntervalHours, "mapSampleIntervalHours", 1);
            Scribe_Values.Look(ref defaultRangePreset, "defaultRangePreset", (int)UI.MonitorRangePreset.Hours24);
            Scribe_Values.Look(ref dayRolloverHour, "dayRolloverHour", 5);
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                dayRolloverHour = NormalizeDayRolloverHour(dayRolloverHour);
            }
            Scribe_Values.Look(ref maxDailyBuckets, "maxDailyBuckets", 20);
            Scribe_Values.Look(ref maxQuadrumBuckets, "maxQuadrumBuckets", 12);
            Scribe_Values.Look(ref maxYearBuckets, "maxYearBuckets", 7);
            Scribe_Values.Look(ref yearHistoryUnlimited, "yearHistoryUnlimited", false);
            Scribe_Values.Look(ref monitorWindowSize, "monitorWindowSize", new Vector2(720f, 520f));
            workGiverSkillOverrideCache = null;
        }
    }
}
