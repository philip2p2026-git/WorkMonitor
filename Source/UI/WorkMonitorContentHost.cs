using UnityEngine;
using Verse;
using WorkMonitor.Groups;

namespace WorkMonitor.UI
{
    public enum MonitorView
    {
        Overview,
        GroupDetail
    }

    public class WorkMonitorContentHost
    {
        private readonly WorkGroupOverviewPanel overviewPanel = new WorkGroupOverviewPanel();
        private readonly WorkGroupDetailPanel detailPanel = new WorkGroupDetailPanel();

        private MonitorView view = MonitorView.Overview;
        private WorkGroupSnapshot selectedGroup;

        public void ResetToOverview()
        {
            view = MonitorView.Overview;
            selectedGroup = null;
        }

        public void Draw(Rect rect)
        {
            if (view == MonitorView.Overview)
            {
                WorkGroupSnapshot clicked = overviewPanel.Draw(rect, out bool rowClicked);
                if (rowClicked && clicked != null)
                {
                    selectedGroup = clicked;
                    detailPanel.SetGroup(selectedGroup);
                    view = MonitorView.GroupDetail;
                }
            }
            else
            {
                detailPanel.Draw(rect, out bool back, out bool highlight);
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
