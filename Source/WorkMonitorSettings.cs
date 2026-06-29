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

        public Vector2 monitorWindowSize = new Vector2(720f, 520f);

        private Dictionary<string, string> skillRoleOverrideCache;

        public int StatsWindowTicks => statsWindowHours * TicksPerHour;
        public int GreenStatusTicks => greenStatusHours * TicksPerHour;
        public int YellowStatusTicks => yellowStatusHours * TicksPerHour;

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
            Scribe_Values.Look(ref monitorWindowSize, "monitorWindowSize", new Vector2(720f, 520f));
            skillRoleOverrideCache = null;
        }
    }
}
