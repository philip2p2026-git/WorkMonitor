using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace WorkMonitor
{
    public static class WorkMonitorUtility
    {
        public static IEnumerable<Pawn> MonitorColonists()
        {
            Map map = Find.CurrentMap;
            if (map == null)
            {
                yield break;
            }

            foreach (Pawn pawn in map.mapPawns.FreeColonists)
            {
                if (pawn.Spawned && !pawn.DevelopmentalStage.Baby())
                {
                    yield return pawn;
                }
            }
        }

        public static string FormatDuration(int ticks, bool asHours)
        {
            if (asHours)
            {
                float hours = ticks / (float)WorkMonitorSettings.TicksPerHour;
                return "WorkMonitor.Hours".Translate(hours.ToString("0.#"));
            }

            return ticks.ToString();
        }

        public static int CurrentTicksGame()
        {
            return Find.TickManager.TicksGame;
        }

        public static int CurrentHourIndex()
        {
            return CurrentTicksGame() / WorkMonitorSettings.TicksPerHour;
        }

        public static float EstimateWorkUnitsForInterval(Pawn pawn, Job job, int delta)
        {
            if (pawn == null || job == null || delta <= 0)
            {
                return 0f;
            }

            float factor = pawn.GetStatValue(StatDefOf.WorkSpeedGlobal);
            WorkTypeDef workType = job.workGiverDef?.workType;
            SkillDef skill = workType?.relevantSkills?.FirstOrDefault();
            if (skill != null && pawn.skills != null)
            {
                factor *= 0.3f + pawn.skills.GetSkill(skill).Level / 20f;
            }

            return factor * delta;
        }

        public static string FormatWorkUnits(float units)
        {
            if (units >= 10000f)
            {
                return (units / 1000f).ToString("0.#") + "k";
            }

            return units.ToString("0");
        }
    }
}
