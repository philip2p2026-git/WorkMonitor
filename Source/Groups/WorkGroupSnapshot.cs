using System.Collections.Generic;
using RimWorld;
using Verse;

namespace WorkMonitor.Groups
{
    public class WorkGroupSnapshot
    {
        public WorkGroupKey Key;
        public string Label;
        public List<WorkGiverDef> WorkGivers = new List<WorkGiverDef>();
        public List<WorkTypeDef> UniqueWorkTypes = new List<WorkTypeDef>();
        public WorkTypeDef PrimaryWorkType;
    }
}
