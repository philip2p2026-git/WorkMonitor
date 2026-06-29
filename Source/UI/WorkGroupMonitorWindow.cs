using UnityEngine;
using Verse;
using WorkMonitor.Groups;
using WorkMonitor.UI;

namespace WorkMonitor
{
    public class WorkGroupMonitorWindow : Window
    {
        private readonly WorkMonitorContentHost contentHost = new WorkMonitorContentHost();

        public WorkGroupMonitorWindow()
        {
            forcePause = false;
            absorbInputAroundWindow = false;
            closeOnClickedOutside = false;
            optionalTitle = "WorkMonitor.ModName".Translate();
        }

        public override Vector2 InitialSize =>
            WorkMonitorMod.Settings?.monitorWindowSize ?? new Vector2(720f, 520f);

        public static void Open()
        {
            if (Find.WindowStack.IsOpen<WorkGroupMonitorWindow>())
            {
                return;
            }

            Find.WindowStack.Add(new WorkGroupMonitorWindow());
        }

        public override void DoWindowContents(Rect inRect)
        {
            contentHost.Draw(inRect);
        }
    }
}
