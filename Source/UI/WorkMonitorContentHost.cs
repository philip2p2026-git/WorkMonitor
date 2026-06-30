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
        WorkGiverDetail,
        ColonistDetail
    }

    public class WorkMonitorContentHost
    {
        private readonly WorkGroupOverviewPanel overviewPanel = new WorkGroupOverviewPanel();
        private readonly WorkGroupDetailPanel detailPanel = new WorkGroupDetailPanel();
        private readonly WorkGiverDetailPanel workGiverDetailPanel = new WorkGiverDetailPanel();
        private readonly ColonistDetailPanel colonistDetailPanel = new ColonistDetailPanel();
        private readonly MonitorRangeState rangeState = new MonitorRangeState();

        private MonitorView view = MonitorView.Overview;
        private WorkGroupSnapshot selectedGroup;
        private WorkGiverDef selectedWorkGiver;
        private Pawn selectedColonist;

        public void ResetToOverview()
        {
            view = MonitorView.Overview;
            selectedGroup = null;
            selectedWorkGiver = null;
            selectedColonist = null;
        }

        public void Draw(Rect rect)
        {
            if (view == MonitorView.Overview)
            {
                WorkGroupSnapshot clicked = overviewPanel.Draw(rect, rangeState, out bool rowClicked);
                if (rowClicked && clicked != null)
                {
                    selectedGroup = clicked;
                    detailPanel.SetGroup(selectedGroup, rangeState);
                    view = MonitorView.GroupDetail;
                }

                return;
            }

            if (view == MonitorView.GroupDetail)
            {
                detailPanel.Draw(rect, rangeState, out bool back, out bool colonistClicked, out ColonistWorkStat selectedColonistStat, out WorkGroupSnapshot groupChanged, out bool workGiverClicked, out WorkGiverDef workGiver);
                if (groupChanged != null)
                {
                    selectedGroup = groupChanged;
                }

                if (back)
                {
                    view = MonitorView.Overview;
                    selectedGroup = null;
                }
                else if (workGiverClicked && workGiver != null)
                {
                    selectedWorkGiver = workGiver;
                    workGiverDetailPanel.SetWorkGiver(selectedGroup, selectedWorkGiver, rangeState);
                    view = MonitorView.WorkGiverDetail;
                }
                else if (colonistClicked && selectedColonistStat?.Pawn != null)
                {
                    selectedColonist = selectedColonistStat.Pawn;
                    colonistDetailPanel.SetColonist(selectedColonist, rangeState, selectedGroup, openGroupDetail: true);
                    view = MonitorView.ColonistDetail;
                }

                return;
            }

            if (view == MonitorView.WorkGiverDetail)
            {
                workGiverDetailPanel.Draw(rect, rangeState, out bool back, out bool colonistClicked, out ColonistWorkStat selectedColonistStat);
                if (back)
                {
                    detailPanel.SetGroup(selectedGroup, rangeState);
                    view = MonitorView.GroupDetail;
                }
                else if (colonistClicked && selectedColonistStat?.Pawn != null)
                {
                    selectedColonist = selectedColonistStat.Pawn;
                    colonistDetailPanel.SetColonist(selectedColonist, rangeState, selectedGroup, openGroupDetail: true);
                    view = MonitorView.ColonistDetail;
                }

                return;
            }

            colonistDetailPanel.Draw(rect, rangeState, out bool colonistBack, out bool groupClicked, out WorkGroupSnapshot groupFromColonist);
            if (groupClicked && groupFromColonist != null)
            {
                selectedGroup = groupFromColonist;
                detailPanel.SetGroup(selectedGroup, rangeState);
                view = MonitorView.GroupDetail;
            }
            else if (colonistBack)
            {
                if (selectedWorkGiver != null)
                {
                    workGiverDetailPanel.SetWorkGiver(selectedGroup, selectedWorkGiver, rangeState);
                    view = MonitorView.WorkGiverDetail;
                }
                else
                {
                    view = MonitorView.GroupDetail;
                }
            }
        }
    }
}
