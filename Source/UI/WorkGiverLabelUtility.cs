using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace WorkMonitor.UI
{
    public static class WorkGiverLabelUtility
    {
        public const float SkillIconSize = 16f;
        public const float SkillIconGap = 4f;

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

            string format = settings.workGiverLabelFormat;
            if (format.NullOrEmpty())
            {
                format = "{label}";
            }

            if (UsesSkillIcon(workGiver))
            {
                return format
                    .Replace("{skill}", "")
                    .Replace("{label}", workGiver.label)
                    .Trim(' ', ':');
            }

            string skillRole = ResolveSkillRole(workGiver);
            if (skillRole.NullOrEmpty())
            {
                return workGiver.label;
            }

            return format
                .Replace("{skill}", skillRole)
                .Replace("{label}", workGiver.label);
        }

        public static bool TryGetSkillDef(WorkGiverDef workGiver, out SkillDef skill)
        {
            skill = null;
            if (workGiver?.workType?.relevantSkills == null || workGiver.workType.relevantSkills.Count == 0)
            {
                return false;
            }

            skill = workGiver.workType.relevantSkills.FirstOrDefault();
            return skill != null;
        }

        public static bool TryDrawSkillIcon(Rect iconRect, WorkGiverDef workGiver)
        {
            if (!TryGetSkillDef(workGiver, out SkillDef skill))
            {
                return false;
            }

            Widgets.DefIcon(iconRect, skill);
            return true;
        }

        public static Texture2D GetSkillIcon(WorkGiverDef workGiver)
        {
            if (!TryGetSkillDef(workGiver, out SkillDef skill))
            {
                return null;
            }

            return ResolveSkillIconTexture(skill);
        }

        private static Texture2D ResolveSkillIconTexture(SkillDef skill)
        {
            if (skill == null)
            {
                return null;
            }

            string[] paths =
            {
                "UI/Icons/Skills/" + skill.defName,
                "UI/Widgets/SkillBar/" + skill.defName,
                "UI/Skills/" + skill.defName,
                "Things/Mote/SkillNeed/" + skill.defName
            };

            foreach (string path in paths)
            {
                Texture2D texture = ContentFinder<Texture2D>.Get(path, false);
                if (texture != null)
                {
                    return texture;
                }
            }

            return null;
        }

        public static bool UsesSkillIcon(WorkGiverDef workGiver)
        {
            WorkMonitorSettings settings = WorkMonitorMod.Settings;
            if (settings == null || !settings.showSkillOnWorkGiverLabels)
            {
                return false;
            }

            if (!WorkGiverSkillUtility.UsesRelevantSkill(workGiver))
            {
                return false;
            }

            string format = settings.workGiverLabelFormat;
            if (format.NullOrEmpty())
            {
                format = "{label}";
            }

            if (format.Contains("{skill}"))
            {
                return false;
            }

            return TryGetSkillDef(workGiver, out _);
        }

        public static float SkillIconReserve(WorkGiverDef workGiver)
        {
            return UsesSkillIcon(workGiver) ? SkillIconSize + SkillIconGap : 0f;
        }

        public static void Draw(Rect row, float labelLeft, float labelWidth, WorkGiverDef workGiver, GameFont font)
        {
            if (workGiver == null)
            {
                return;
            }

            Text.Font = font;
            float textLeft = labelLeft;
            float textWidth = labelWidth;

            if (UsesSkillIcon(workGiver))
            {
                Rect iconRect = new Rect(
                    labelLeft,
                    row.y + (row.height - SkillIconSize) * 0.5f,
                    SkillIconSize,
                    SkillIconSize);

                if (TryDrawSkillIcon(iconRect, workGiver))
                {
                    textLeft += SkillIconSize + SkillIconGap;
                    textWidth -= SkillIconSize + SkillIconGap;
                }
                else
                {
                    Texture2D icon = GetSkillIcon(workGiver);
                    if (icon != null)
                    {
                        Widgets.DrawTextureFitted(iconRect, icon, 1f);
                        textLeft += SkillIconSize + SkillIconGap;
                        textWidth -= SkillIconSize + SkillIconGap;
                    }
                }
            }

            if (textWidth > 0f)
            {
                Widgets.Label(new Rect(textLeft, row.y, textWidth, row.height), Format(workGiver).Truncate(textWidth));
            }
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
