using System;
using Verse;

namespace WorkMonitor.UI
{
    public static class BulkExpandUtility
    {
        public static string BulkButtonLabel(bool allLevel2Expanded)
        {
            return allLevel2Expanded
                ? "WorkMonitor.CollapseAll".Translate()
                : "WorkMonitor.ExpandAll".Translate();
        }

        public static void ApplyBulkToggle(
            bool allLevel2Expanded,
            Action expandOneLevel,
            Action collapseOneLevel)
        {
            if (allLevel2Expanded)
            {
                collapseOneLevel();
            }
            else
            {
                expandOneLevel();
            }
        }

        public static void ExpandOneLevel(
            bool allLevel1Expanded,
            Action expandLevel1,
            Action expandLevel2)
        {
            if (!allLevel1Expanded)
            {
                expandLevel1();
            }
            else
            {
                expandLevel2();
            }
        }

        public static void CollapseOneLevel(
            bool anyLevel2Expanded,
            Action collapseLevel2,
            Action collapseLevel1)
        {
            if (anyLevel2Expanded)
            {
                collapseLevel2();
            }
            else
            {
                collapseLevel1();
            }
        }
    }
}
