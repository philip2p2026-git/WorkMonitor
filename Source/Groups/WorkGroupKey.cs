using RimWorld;
using Verse;

namespace WorkMonitor.Groups
{
    public enum WorkGroupKind
    {
        WorkType,
        CustomGroup,
        Other
    }

    public struct WorkGroupKey
    {
        public WorkGroupKind Kind;
        public string Id;

        public string StorageKey => Kind + ":" + Id;

        public static WorkGroupKey ForWorkType(WorkTypeDef workType)
        {
            return new WorkGroupKey
            {
                Kind = WorkGroupKind.WorkType,
                Id = workType?.defName ?? string.Empty
            };
        }

        public static WorkGroupKey ForCustomGroup(string defName)
        {
            return new WorkGroupKey
            {
                Kind = WorkGroupKind.CustomGroup,
                Id = defName ?? string.Empty
            };
        }

        public static WorkGroupKey ForOther()
        {
            return new WorkGroupKey
            {
                Kind = WorkGroupKind.Other,
                Id = "Other"
            };
        }
    }
}
