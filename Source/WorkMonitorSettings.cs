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
        public bool showSkillOnWorkGiverLabels = true;
        public string workGiverLabelFormat = "{skill}: {label}";
        public string skillRoleOverrides = "";
        public string workGiverSkillOverrides = "";
        public int mapSampleIntervalHours = 6;
        public int defaultRangePreset = (int)UI.MonitorRangePreset.Hours24;
        public int dayRolloverHour = 0;
        public int maxDailyBuckets = 20;
        public int maxQuadrumBuckets = 12;
        public int maxYearBuckets = 7;
        public bool yearHistoryUnlimited = false;

        public UI.MonitorRangePreset DefaultRangePreset => (UI.MonitorRangePreset)defaultRangePreset;

        public Vector2 monitorWindowSize = new Vector2(720f, 520f);

        private Dictionary<string, string> skillRoleOverrideCache;
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

        public bool TryGetSkillRoleOverride(string skillDefName, out string label)
        {
            label = null;
            if (skillDefName.NullOrEmpty())
            {
                return false;
            }

            EnsureOverrideCache();
            return skillRoleOverrideCache.TryGetValue(skillDefName, out label);
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

        private void EnsureOverrideCache()
        {
            if (skillRoleOverrideCache != null)
            {
                return;
            }

            skillRoleOverrideCache = new Dictionary<string, string>();
            if (skillRoleOverrides.NullOrEmpty())
            {
                return;
            }

            string[] pairs = skillRoleOverrides.Split(',');
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
                if (!key.NullOrEmpty() && !value.NullOrEmpty())
                {
                    skillRoleOverrideCache[key] = value;
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
            Scribe_Values.Look(ref showSkillOnWorkGiverLabels, "showSkillOnWorkGiverLabels", true);
            Scribe_Values.Look(ref workGiverLabelFormat, "workGiverLabelFormat", "{skill}: {label}");
            Scribe_Values.Look(ref skillRoleOverrides, "skillRoleOverrides", "");
            Scribe_Values.Look(ref workGiverSkillOverrides, "workGiverSkillOverrides", "");
            Scribe_Values.Look(ref mapSampleIntervalHours, "mapSampleIntervalHours", 6);
            Scribe_Values.Look(ref defaultRangePreset, "defaultRangePreset", (int)UI.MonitorRangePreset.Hours24);
            Scribe_Values.Look(ref dayRolloverHour, "dayRolloverHour", 0);
            Scribe_Values.Look(ref maxDailyBuckets, "maxDailyBuckets", 20);
            Scribe_Values.Look(ref maxQuadrumBuckets, "maxQuadrumBuckets", 12);
            Scribe_Values.Look(ref maxYearBuckets, "maxYearBuckets", 7);
            Scribe_Values.Look(ref yearHistoryUnlimited, "yearHistoryUnlimited", false);
            Scribe_Values.Look(ref monitorWindowSize, "monitorWindowSize", new Vector2(720f, 520f));
            skillRoleOverrideCache = null;
            workGiverSkillOverrideCache = null;
        }
    }
}
