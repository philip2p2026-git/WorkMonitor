using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using WorkMonitor.Tracking;

namespace WorkMonitor.Groups
{
    public static class ColonistWorkQuery
    {
        public static List<int> GetColonistIdsForGroup(WorkGroupSnapshot group, int minHourIndex)
        {
            var ids = new HashSet<int>();
            WorkActivityTracker tracker = WorkActivityTracker.EnsureRegistered();

            foreach (Pawn pawn in WorkMonitorUtility.MonitorColonists())
            {
                ids.Add(pawn.thingIDNumber);
            }

            if (tracker != null && group != null)
            {
                foreach (int pawnId in tracker.GetPawnIdsWithWorkForGroup(group, minHourIndex))
                {
                    ids.Add(pawnId);
                }
            }

            return ids.ToList();
        }

        public static List<int> GetColonistIdsForWorkGiver(WorkGiverDef workGiver, int minHourIndex)
        {
            var ids = new HashSet<int>();
            WorkActivityTracker tracker = WorkActivityTracker.EnsureRegistered();

            foreach (Pawn pawn in WorkMonitorUtility.MonitorColonists())
            {
                ids.Add(pawn.thingIDNumber);
            }

            if (tracker != null && workGiver != null)
            {
                foreach (int pawnId in tracker.GetPawnIdsWithWorkForWorkGiver(workGiver.defName, minHourIndex))
                {
                    ids.Add(pawnId);
                }
            }

            return ids.ToList();
        }

        public static List<int> GetColonistIdsWithAnyWork(int minHourIndex)
        {
            var ids = new HashSet<int>();
            WorkActivityTracker tracker = WorkActivityTracker.EnsureRegistered();

            foreach (Pawn pawn in WorkMonitorUtility.MonitorColonists())
            {
                ids.Add(pawn.thingIDNumber);
            }

            if (tracker == null)
            {
                return ids.ToList();
            }

            foreach (WorkGroupSnapshot group in WorkGroupRegistry.GetAllGroups())
            {
                foreach (int pawnId in tracker.GetPawnIdsWithWorkForGroup(group, minHourIndex))
                {
                    ids.Add(pawnId);
                }
            }

            return ids.ToList();
        }

        public static Pawn TryResolvePawn(int pawnId)
        {
            if (pawnId <= 0)
            {
                return null;
            }

            foreach (Pawn pawn in WorkMonitorUtility.MonitorColonists())
            {
                if (pawn.thingIDNumber == pawnId)
                {
                    return pawn;
                }
            }

            foreach (Pawn pawn in Find.WorldPawns.AllPawnsAliveOrDead)
            {
                if (pawn.thingIDNumber == pawnId)
                {
                    return pawn;
                }
            }

            foreach (Map map in Find.Maps)
            {
                if (map?.mapPawns == null)
                {
                    continue;
                }

                foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
                {
                    if (pawn.thingIDNumber == pawnId)
                    {
                        return pawn;
                    }
                }
            }

            return null;
        }

        public static string ResolveLabel(int pawnId, WorkActivityTracker tracker)
        {
            Pawn pawn = TryResolvePawn(pawnId);
            if (pawn != null)
            {
                return pawn.LabelShort;
            }

            if (tracker != null && tracker.TryGetColonistProfile(pawnId, out ColonistWorkProfile profile)
                && !profile.labelShort.NullOrEmpty())
            {
                return profile.labelShort;
            }

            return "Pawn #" + pawnId;
        }

        public static bool IsAbsent(int pawnId, WorkActivityTracker tracker)
        {
            if (tracker != null && tracker.TryGetColonistProfile(pawnId, out ColonistWorkProfile profile))
            {
                return profile.IsAbsent;
            }

            return !WorkMonitorUtility.MonitorColonists().Any(p => p.thingIDNumber == pawnId);
        }

        public static Passion ResolvePassionForGroup(int pawnId, WorkGroupSnapshot group)
        {
            Pawn pawn = TryResolvePawn(pawnId);
            if (pawn == null || group?.UniqueWorkTypes == null)
            {
                return Passion.None;
            }

            Passion max = Passion.None;
            foreach (WorkTypeDef workType in group.UniqueWorkTypes)
            {
                if (workType == null)
                {
                    continue;
                }

                Passion p = pawn.skills.MaxPassionOfRelevantSkillsFor(workType);
                if ((int)p > (int)max)
                {
                    max = p;
                }
            }

            return max;
        }

        public static Passion ResolvePassionForWorkGiver(int pawnId, WorkGiverDef workGiver)
        {
            Pawn pawn = TryResolvePawn(pawnId);
            if (pawn == null || workGiver?.workType == null)
            {
                return Passion.None;
            }

            return pawn.skills.MaxPassionOfRelevantSkillsFor(workGiver.workType);
        }
    }
}
