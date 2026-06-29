using RimWorld;
using UnityEngine;
using Verse;
using WorkMonitor.Groups;

namespace WorkMonitor.UI
{
    public enum MonitorView
    {
        Overview,
        GroupDetail,
        ColonistDetail
    }

    public class WorkMonitorContentHost
    {
        private readonly WorkGroupOverviewPanel overviewPanel = new WorkGroupOverviewPanel();
        private readonly WorkGroupDetailPanel detailPanel = new WorkGroupDetailPanel();
        private readonly ColonistDetailPanel colonistDetailPanel = new ColonistDetailPanel();

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

                return;
            }

            if (view == MonitorView.GroupDetail)
            {
                detailPanel.Draw(rect, out bool back, out bool highlight, out bool colonistClicked, out ColonistWorkStat selectedColonist);
                if (back)
                {
                    view = MonitorView.Overview;
                    selectedGroup = null;
                }
                else if (colonistClicked && selectedColonist?.Pawn != null)
                {
                    colonistDetailPanel.SetColonist(selectedColonist.Pawn);
                    view = MonitorView.ColonistDetail;
                }
                else if (highlight && selectedGroup != null)
                {
                    WorkTabHighlightController.HighlightGroup(selectedGroup);
                }

                return;
            }

            colonistDetailPanel.Draw(rect, out bool colonistBack);
            if (colonistBack)
            {
                view = MonitorView.GroupDetail;
            }
        }
    }
}
