using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace WorkMonitor.UI
{
    public static class WorkGiverSkillUtility
    {
        private static readonly HashSet<string> NonSkillGiverClassNames = new HashSet<string>
        {
            "WorkGiver_TakeToPen",
            "WorkGiver_TakeRoamingAnimalsToPen",
            "WorkGiver_RebalanceAnimalsInPens",
            "WorkGiver_Haul",
            "WorkGiver_HaulCorpse",
            "WorkGiver_HaulToCellStorage",
            "WorkGiver_HaulToContainer",
            "WorkGiver_Clean",
            "WorkGiver_Flick",
            "WorkGiver_Strip",
            "WorkGiver_Deconstruct",
            "WorkGiver_Repair",
            "WorkGiver_FeedPatient",
            "WorkGiver_TakeToBed",
            "WorkGiver_TakeToBedToOperate",
            "WorkGiver_PatientGoToBed",
            "WorkGiver_PatientGoToBedRecuperate",
            "WorkGiver_Warden",
            "WorkGiver_Warden_Interrogate",
            "WorkGiver_Warden_DeliverFood",
            "WorkGiver_Warden_DoExecution",
            "WorkGiver_Warden_ReleasePrisoner",
            "WorkGiver_Warden_TakeToBed",
            "WorkGiver_Warden_Feed",
            "WorkGiver_Warden_Escort",
            "WorkGiver_EnterTransporter",
            "WorkGiver_LoadTransporters",
            "WorkGiver_UnloadCarriers",
            "WorkGiver_Refuel",
            "WorkGiver_RefuelAtomic",
            "WorkGiver_ExtinguishFires",
            "WorkGiver_RescueDowned",
            "WorkGiver_TakeBeerOutOfFermentingBarrel",
            "WorkGiver_FillFermentingBarrel",
            "WorkGiver_Merge",
            "WorkGiver_Install",
            "WorkGiver_Uninstall",
            "WorkGiver_ConstructDeliverResourcesToBlueprints",
            "WorkGiver_ConstructDeliverResourcesToFrames",
            "WorkGiver_ConstructFinishFrames",
            "WorkGiver_ConstructRemoveFloor",
            "WorkGiver_ConstructSmoothFloor",
            "WorkGiver_ConstructSmoothWall",
            "WorkGiver_RemoveBuilding",
            "WorkGiver_FixBrokenDownBuilding",
            "WorkGiver_ClearSnow",
            "WorkGiver_PruneGlower",
            "WorkGiver_PaintBuilding",
            "WorkGiver_PaintFloor",
            "WorkGiver_RemovePaintBuilding",
            "WorkGiver_RemovePaintFloor",
            "WorkGiver_Strip",
            "WorkGiver_ReleaseAnimalsToWild"
        };

        public static bool UsesRelevantSkill(WorkGiverDef workGiver)
        {
            if (workGiver?.workType == null)
            {
                return false;
            }

            if (workGiver.workType.relevantSkills == null || workGiver.workType.relevantSkills.Count == 0)
            {
                return false;
            }

            WorkMonitorSettings settings = WorkMonitorMod.Settings;
            if (settings != null && settings.TryGetWorkGiverSkillOverride(workGiver.defName, out bool overrideValue))
            {
                return overrideValue;
            }

            string giverClassName = workGiver.giverClass?.Name;
            if (!giverClassName.NullOrEmpty() && NonSkillGiverClassNames.Contains(giverClassName))
            {
                return false;
            }

            if (workGiver.tagToGive == JobTag.Fieldwork)
            {
                return true;
            }

            if (typeof(WorkGiver_DoBill).IsAssignableFrom(workGiver.giverClass))
            {
                return true;
            }

            return true;
        }
    }
}
