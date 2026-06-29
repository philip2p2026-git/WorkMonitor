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
        private Pawn selectedColonist;

        public void ResetToOverview()
        {
            view = MonitorView.Overview;
            selectedGroup = null;
            selectedColonist = null;
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
                detailPanel.Draw(rect, out bool back, out bool colonistClicked, out ColonistWorkStat selectedColonistStat, out WorkGroupSnapshot groupChanged);
                if (groupChanged != null)
                {
                    selectedGroup = groupChanged;
                }

                if (back)
                {
                    view = MonitorView.Overview;
                    selectedGroup = null;
                }
                else if (colonistClicked && selectedColonistStat?.Pawn != null)
                {
                    selectedColonist = selectedColonistStat.Pawn;
                    colonistDetailPanel.SetColonist(selectedColonist, selectedGroup, openGroupDetail: true);
                    view = MonitorView.ColonistDetail;
                }

                return;
            }

            colonistDetailPanel.Draw(rect, out bool colonistBack, out bool groupClicked, out WorkGroupSnapshot groupFromColonist);
            if (groupClicked && groupFromColonist != null)
            {
                selectedGroup = groupFromColonist;
                detailPanel.SetGroup(selectedGroup);
                view = MonitorView.GroupDetail;
            }
            else if (colonistBack)
            {
                view = MonitorView.GroupDetail;
            }
        }
    }
}
