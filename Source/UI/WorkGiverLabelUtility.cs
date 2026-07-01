using System.Linq;
using RimWorld;
using UnityEngine;
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

            if (!ShouldMarkSkill(workGiver))
            {
                return workGiver.label;
            }

            WorkMonitorSettings settings = WorkMonitorMod.Settings;
            WorkGiverSkillMarkerMode mode = settings?.skillMarkerMode ?? WorkGiverSkillMarkerMode.Parentheses;
            switch (mode)
            {
                case WorkGiverSkillMarkerMode.Parentheses:
                {
                    string skillLabel = ResolveSkillLabel(workGiver);
                    return skillLabel.NullOrEmpty()
                        ? workGiver.label
                        : "(" + skillLabel + ") " + workGiver.label;
                }
                case WorkGiverSkillMarkerMode.Asterisk:
                    return "* " + workGiver.label;
                default:
                    return workGiver.label;
            }
        }

        public static void Draw(Rect row, float labelLeft, float labelWidth, WorkGiverDef workGiver, GameFont font)
        {
            if (workGiver == null)
            {
                return;
            }

            Text.Font = font;
            Widgets.Label(new Rect(labelLeft, row.y, labelWidth, row.height), Format(workGiver).Truncate(labelWidth));
        }

        public static void DrawLabelOrText(Rect row, float labelLeft, float labelWidth, WorkGiverDef workGiver, string fallbackLabel, GameFont font)
        {
            if (workGiver != null)
            {
                Draw(row, labelLeft, labelWidth, workGiver, font);
                return;
            }

            if (fallbackLabel.NullOrEmpty())
            {
                return;
            }

            Text.Font = font;
            Widgets.Label(new Rect(labelLeft, row.y, labelWidth, row.height), fallbackLabel.Truncate(labelWidth));
        }

        private static bool ShouldMarkSkill(WorkGiverDef workGiver)
        {
            WorkMonitorSettings settings = WorkMonitorMod.Settings;
            if (settings == null || settings.skillMarkerMode == WorkGiverSkillMarkerMode.Off)
            {
                return false;
            }

            return WorkGiverSkillUtility.UsesRelevantSkill(workGiver);
        }

        private static string ResolveSkillLabel(WorkGiverDef workGiver)
        {
            WorkTypeDef workType = workGiver?.workType;
            if (workType == null)
            {
                return "";
            }

            SkillDef skill = workType.relevantSkills?.FirstOrDefault();
            if (skill == null)
            {
                return workType.pawnLabel ?? workType.labelShort ?? "";
            }

            return skill.label;
        }
    }
}
