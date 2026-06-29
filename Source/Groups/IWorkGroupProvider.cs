using System.Collections.Generic;

namespace WorkMonitor.Groups
{
    public interface IWorkGroupProvider
    {
        IEnumerable<WorkGroupSnapshot> GetGroups();
    }
}
