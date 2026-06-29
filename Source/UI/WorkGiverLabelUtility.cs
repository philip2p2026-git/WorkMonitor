using System.Linq;
using RimWorld;
using Verse;

namespace WorkMonitor.UI
{
    public static class WorkGiverLabelUtility
    {
        public static string Format(WorkGiverDef workGiver)
        {
            if (workGiver == null)
            {
                return "";
            }

            WorkMonitorSettings settings = WorkMonitorMod.Settings;
            if (settings == null || !settings.showSkillOnWorkGiverLabels)
            {
                return workGiver.label;
            }

            if (!WorkGiverSkillUtility.UsesRelevantSkill(workGiver))
            {
                return workGiver.label;
            }

            string skillRole = ResolveSkillRole(workGiver);
            if (skillRole.NullOrEmpty())
            {
                return workGiver.label;
            }

            string format = settings.workGiverLabelFormat;
            if (format.NullOrEmpty())
            {
                format = "{skill}: {label}";
            }

            return format
                .Replace("{skill}", skillRole)
                .Replace("{label}", workGiver.label);
        }

        private static string ResolveSkillRole(WorkGiverDef workGiver)
        {
            WorkTypeDef workType = workGiver.workType;
            if (workType == null)
            {
                return "";
            }

            SkillDef skill = workType.relevantSkills?.FirstOrDefault();
            if (skill == null)
            {
                return workType.pawnLabel ?? workType.labelShort ?? "";
            }

            WorkMonitorSettings settings = WorkMonitorMod.Settings;
            if (settings != null && settings.TryGetSkillRoleOverride(skill.defName, out string custom))
            {
                return custom;
            }

            return skill.label;
        }
    }
}
