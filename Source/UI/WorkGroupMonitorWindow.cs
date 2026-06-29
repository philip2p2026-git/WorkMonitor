using UnityEngine;
using Verse;
using WorkMonitor.Groups;
using WorkMonitor.UI;

namespace WorkMonitor
{
    public enum MonitorView
    {
        Overview,
        GroupDetail
    }

    public class WorkGroupMonitorWindow : Window
    {
        private readonly WorkGroupOverviewPanel overviewPanel = new WorkGroupOverviewPanel();
        private readonly WorkGroupDetailPanel detailPanel = new WorkGroupDetailPanel();

        private MonitorView view = MonitorView.Overview;
        private WorkGroupSnapshot selectedGroup;

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
            if (view == MonitorView.Overview)
            {
                WorkGroupSnapshot clicked = overviewPanel.Draw(inRect, out bool rowClicked);
                if (rowClicked && clicked != null)
                {
                    selectedGroup = clicked;
                    detailPanel.SetGroup(selectedGroup);
                    view = MonitorView.GroupDetail;
                }
            }
            else
            {
                detailPanel.Draw(inRect, out bool back, out bool highlight);
                if (back)
                {
                    view = MonitorView.Overview;
                    selectedGroup = null;
                }
                else if (highlight && selectedGroup != null)
                {
                    WorkTabHighlightController.HighlightGroup(selectedGroup);
                }
            }
        }
    }
}
