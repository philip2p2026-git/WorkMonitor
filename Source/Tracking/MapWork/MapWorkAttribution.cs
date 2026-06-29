using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using WorkMonitor.Groups;

namespace WorkMonitor.Tracking.MapWork
{
    public static class MapWorkAttribution
    {
        public static WorkGiverDef GetWorkGiver(string defName)
        {
            return defName.NullOrEmpty()
                ? null
                : DefDatabase<WorkGiverDef>.GetNamedSilentFail(defName);
        }

        public static List<string> GroupKeysFor(IEnumerable<WorkGiverDef> workGivers)
        {
            if (workGivers == null)
            {
                return new List<string>();
            }

            return workGivers
                .Where(wg => wg != null)
                .SelectMany(WorkGroupKeyResolver.ResolveGroupKeysForWorkGiver)
                .Distinct()
                .ToList();
        }

        public static List<WorkGiverDef> WorkGiversFor(params string[] defNames)
        {
            List<WorkGiverDef> result = new List<WorkGiverDef>();
            foreach (string defName in defNames)
            {
                WorkGiverDef wg = GetWorkGiver(defName);
                if (wg != null)
                {
                    result.Add(wg);
                }
            }

            return result;
        }

        public static List<WorkGiverDef> ResolveWorkGiversForBillGiver(Thing billGiverThing, Bill bill)
        {
            List<WorkGiverDef> result = new List<WorkGiverDef>();
            foreach (WorkGiverDef wg in DefDatabase<WorkGiverDef>.AllDefsListForReading)
            {
                if (wg.fixedBillGiverDefs != null && wg.fixedBillGiverDefs.Contains(billGiverThing.def))
                {
                    result.Add(wg);
                }
            }

            if (result.Count == 0 && bill.recipe?.workSkill != null)
            {
                WorkTypeDef workType = WorkTypeForSkill(bill.recipe.workSkill);
                if (workType != null)
                {
                    result.AddRange(PrimaryWorkGiversForWorkType(workType));
                }
            }

            return result.Distinct().ToList();
        }

        public static List<WorkGiverDef> ResolveWorkGiversForUnfinished(UnfinishedThing unfinished)
        {
            if (unfinished.BoundBill != null)
            {
                return new List<WorkGiverDef>();
            }

            if (unfinished.Recipe?.workSkill != null)
            {
                WorkTypeDef workType = WorkTypeForSkill(unfinished.Recipe.workSkill);
                if (workType != null)
                {
                    return PrimaryWorkGiversForWorkType(workType);
                }
            }

            return new List<WorkGiverDef>();
        }

        public static WorkTypeDef WorkTypeForSkill(SkillDef skill)
        {
            if (skill == null)
            {
                return null;
            }

            foreach (WorkTypeDef workType in DefDatabase<WorkTypeDef>.AllDefsListForReading)
            {
                if (workType.relevantSkills != null && workType.relevantSkills.Contains(skill))
                {
                    return workType;
                }
            }

            return null;
        }

        public static List<WorkGiverDef> PrimaryWorkGiversForWorkType(WorkTypeDef workType)
        {
            if (workType?.workGiversByPriority == null || workType.workGiversByPriority.Count == 0)
            {
                return new List<WorkGiverDef>();
            }

            return new List<WorkGiverDef> { workType.workGiversByPriority[0] };
        }

        public static WorkGiverDef ConstructFinishFramesWorkGiver()
        {
            return GetWorkGiver("ConstructFinishFrames") ?? PrimaryWorkGiversForWorkType(WorkTypeDefOf.Construction).FirstOrDefault();
        }

        public static WorkGiverDef MineWorkGiver()
        {
            return GetWorkGiver("Mine") ?? PrimaryWorkGiversForWorkType(WorkTypeDefOf.Mining).FirstOrDefault();
        }

        public static WorkGiverDef DrillWorkGiver()
        {
            return GetWorkGiver("Drill");
        }
    }
}
