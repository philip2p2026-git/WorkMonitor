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
        private int selectedColonistPawnId;

        public void ResetToOverview()
        {
            view = MonitorView.Overview;
            selectedGroup = null;
            selectedWorkGiver = null;
            selectedColonistPawnId = 0;
            overviewPanel.ClearExpandCaches();
        }

        public void Draw(Rect rect)
        {
            if (view == MonitorView.Overview)
            {
                overviewPanel.Draw(
                    rect,
                    rangeState,
                    out bool groupClicked,
                    out WorkGroupSnapshot clickedGroup,
                    out bool colonistClicked,
                    out ColonistWorkStat selectedColonistStat,
                    out WorkGroupSnapshot colonistGroup,
                    out bool workGiverClicked,
                    out WorkGiverDef workGiver,
                    out WorkGroupSnapshot workGiverGroup);

                if (workGiverClicked && workGiver != null && workGiverGroup != null)
                {
                    selectedGroup = workGiverGroup;
                    selectedWorkGiver = workGiver;
                    workGiverDetailPanel.SetWorkGiver(selectedGroup, selectedWorkGiver, rangeState);
                    view = MonitorView.WorkGiverDetail;
                }
                else if (colonistClicked && selectedColonistStat != null && selectedColonistStat.PawnId > 0)
                {
                    selectedColonistPawnId = selectedColonistStat.PawnId;
                    selectedWorkGiver = null;
                    if (colonistGroup != null)
                    {
                        selectedGroup = colonistGroup;
                        colonistDetailPanel.SetColonist(selectedColonistPawnId, rangeState, selectedGroup, openGroupDetail: true, returnGroup: selectedGroup);
                    }
                    else
                    {
                        colonistDetailPanel.SetColonist(selectedColonistPawnId, rangeState, openGroupDetail: false);
                    }

                    view = MonitorView.ColonistDetail;
                }
                else if (groupClicked && clickedGroup != null)
                {
                    selectedGroup = clickedGroup;
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
                else if (colonistClicked && selectedColonistStat != null && selectedColonistStat.PawnId > 0)
                {
                    selectedColonistPawnId = selectedColonistStat.PawnId;
                    selectedWorkGiver = null;
                    colonistDetailPanel.SetColonist(selectedColonistPawnId, rangeState, selectedGroup, openGroupDetail: true, returnGroup: selectedGroup);
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
                else if (colonistClicked && selectedColonistStat != null && selectedColonistStat.PawnId > 0)
                {
                    selectedColonistPawnId = selectedColonistStat.PawnId;
                    colonistDetailPanel.SetColonist(selectedColonistPawnId, rangeState, selectedGroup, openGroupDetail: true, returnGroup: selectedGroup, returnWorkGiver: selectedWorkGiver);
                    view = MonitorView.ColonistDetail;
                }

                return;
            }

            colonistDetailPanel.Draw(rect, rangeState, out bool colonistBack, out bool groupClickedFromColonist, out WorkGroupSnapshot groupFromColonist);
            if (groupClickedFromColonist && groupFromColonist != null)
            {
                selectedGroup = groupFromColonist;
                detailPanel.SetGroup(selectedGroup, rangeState);
                view = MonitorView.GroupDetail;
            }
            else if (colonistBack)
            {
                if (selectedWorkGiver != null && selectedGroup != null)
                {
                    workGiverDetailPanel.SetWorkGiver(selectedGroup, selectedWorkGiver, rangeState);
                    view = MonitorView.WorkGiverDetail;
                }
                else if (selectedGroup != null)
                {
                    detailPanel.SetGroup(selectedGroup, rangeState);
                    view = MonitorView.GroupDetail;
                }
                else
                {
                    view = MonitorView.Overview;
                    selectedGroup = null;
                    selectedWorkGiver = null;
                }
            }
        }
    }
}
