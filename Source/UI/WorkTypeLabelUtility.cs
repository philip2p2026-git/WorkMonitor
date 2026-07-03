using RimWorld;
using Verse;

namespace WorkMonitor.UI
{
    public static class WorkTypeLabelUtility
    {
        public static string Format(WorkTypeDef workType)
        {
            if (workType == null)
            {
                return "";
            }

            if (!workType.label.NullOrEmpty())
            {
                return workType.label;
            }

            if (!workType.labelShort.NullOrEmpty())
            {
                return workType.labelShort;
            }

            if (!workType.pawnLabel.NullOrEmpty())
            {
                return workType.pawnLabel;
            }

            return workType.defName ?? "";
        }
    }
}
