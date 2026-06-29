using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using WorkMonitor.Groups;

namespace WorkMonitor.Tracking
{
    public class MapWorkSampler : GameComponent
    {
        private static MapWorkSampler instance;

        private MapWorkSnapshot latestSnapshot;
        private int lastSampledHour = -1;
        private List<MapWorkSnapshot> historyBuffer = new List<MapWorkSnapshot>();

        public static MapWorkSampler Instance
        {
            get
            {
                if (instance == null && Current.Game != null)
                {
                    instance = Current.Game.GetComponent<MapWorkSampler>();
                }

                return instance;
            }
        }

        public static MapWorkSampler EnsureRegistered()
        {
            if (Current.Game == null)
            {
                return null;
            }

            MapWorkSampler sampler = Current.Game.GetComponent<MapWorkSampler>();
            if (sampler == null)
            {
                sampler = new MapWorkSampler(Current.Game);
                Current.Game.components.Add(sampler);
            }

            instance = sampler;
            return sampler;
        }

        public static void ClearInstance()
        {
            instance = null;
        }

        public MapWorkSampler(Game game)
        {
            instance = this;
        }

        public MapWorkSnapshot GetLatestSnapshot()
        {
            return latestSnapshot;
        }

        public override void GameComponentTick()
        {
            if (Find.TickManager.TicksGame % 250 != 0)
            {
                return;
            }

            TrySampleIfDue();
        }

        public void TrySampleIfDue(bool force = false)
        {
            Map map = Find.CurrentMap;
            if (map == null)
            {
                return;
            }

            int hour = WorkMonitorUtility.CurrentHourIndex();
            int interval = NormalizeInterval(WorkMonitorMod.Settings?.mapSampleIntervalHours ?? 6);
            if (!force && hour % interval != 0)
            {
                return;
            }

            if (!force && lastSampledHour == hour)
            {
                return;
            }

            lastSampledHour = hour;
            latestSnapshot = BuildSnapshot(map, hour, Find.TickManager.TicksGame);
            historyBuffer.Add(latestSnapshot);
            while (historyBuffer.Count > 48)
            {
                historyBuffer.RemoveAt(0);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref latestSnapshot, "latestSnapshot");
            Scribe_Collections.Look(ref historyBuffer, "historyBuffer", LookMode.Deep);
            Scribe_Values.Look(ref lastSampledHour, "lastSampledHour", -1);
            historyBuffer ??= new List<MapWorkSnapshot>();
        }

        public static int NormalizeInterval(int hours)
        {
            return hours switch
            {
                1 => 1,
                2 => 2,
                3 => 3,
                12 => 12,
                _ => 6
            };
        }

        private static MapWorkSnapshot BuildSnapshot(Map map, int hourIndex, int sampleTick)
        {
            MapWorkSnapshot snapshot = new MapWorkSnapshot
            {
                hourIndex = hourIndex,
                sampleTick = sampleTick
            };

            List<ScannedMapTarget> targets = new List<ScannedMapTarget>();
            ScanBills(map, targets);
            ScanFrames(map, targets);
            ScanMineables(map, targets);
            ScanUnfinishedThings(map, targets);

            foreach (ScannedMapTarget target in targets)
            {
                foreach (string workGiverDefName in target.WorkGiverDefNames)
                {
                    MapWorkGiverSnapshot wgSnap = snapshot.GetOrCreateWorkGiver(workGiverDefName);
                    wgSnap.openTaskCount++;
                    wgSnap.workLeftTotal += target.WorkLeft;
                }

                foreach (string groupKey in target.GroupKeys)
                {
                    MapWorkGroupSnapshot groupSnap = snapshot.GetOrCreateGroup(groupKey);
                    groupSnap.openTaskCount++;
                    groupSnap.workLeftTotal += target.WorkLeft;
                }
            }

            return snapshot;
        }

        private static void ScanBills(Map map, List<ScannedMapTarget> targets)
        {
            foreach (Thing thing in map.listerThings.AllThings)
            {
                if (thing is not IBillGiver billGiver || billGiver.BillStack == null)
                {
                    continue;
                }

                foreach (Bill bill in billGiver.BillStack)
                {
                    if (!WorkLeftResolver.TryGetBillBacklog(bill, out float workLeft, out bool countable) || !countable)
                    {
                        continue;
                    }

                    List<WorkGiverDef> workGivers = ResolveWorkGiversForBillGiver(thing, bill);
                    if (workGivers.Count == 0)
                    {
                        continue;
                    }

                    targets.Add(new ScannedMapTarget(
                        "bill:" + bill.GetUniqueLoadID(),
                        workLeft,
                        workGivers,
                        workGivers.SelectMany(WorkGroupKeyResolver.ResolveGroupKeysForWorkGiver).Distinct().ToList()));
                }
            }
        }

        private static void ScanFrames(Map map, List<ScannedMapTarget> targets)
        {
            foreach (Thing thing in map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingFrame))
            {
                if (thing is not Frame frame || frame.WorkLeft <= 0f)
                {
                    continue;
                }

                List<WorkGiverDef> workGivers = PrimaryWorkGiversForWorkType(WorkTypeDefOf.Construction);
                if (workGivers.Count == 0)
                {
                    continue;
                }

                targets.Add(new ScannedMapTarget(
                    "frame:" + frame.thingIDNumber,
                    frame.WorkLeft,
                    workGivers,
                    workGivers.SelectMany(WorkGroupKeyResolver.ResolveGroupKeysForWorkGiver).Distinct().ToList()));
            }
        }

        private static void ScanMineables(Map map, List<ScannedMapTarget> targets)
        {
            DesignationDef mineDef = DesignationDefOf.Mine;
            if (mineDef == null)
            {
                return;
            }

            foreach (Designation designation in map.designationManager.SpawnedDesignationsOfDef(mineDef))
            {
                if (designation.target.Thing is not Mineable mineable || !mineable.Spawned)
                {
                    continue;
                }

                if (!WorkLeftResolver.TryGetThingWorkLeft(mineable, out float workLeft))
                {
                    continue;
                }

                List<WorkGiverDef> workGivers = PrimaryWorkGiversForWorkType(WorkTypeDefOf.Mining);
                if (workGivers.Count == 0)
                {
                    continue;
                }

                targets.Add(new ScannedMapTarget(
                    "mine:" + mineable.thingIDNumber,
                    workLeft,
                    workGivers,
                    workGivers.SelectMany(WorkGroupKeyResolver.ResolveGroupKeysForWorkGiver).Distinct().ToList()));
            }
        }

        private static void ScanUnfinishedThings(Map map, List<ScannedMapTarget> targets)
        {
            foreach (Thing thing in map.listerThings.AllThings)
            {
                if (thing is not UnfinishedThing unfinished || unfinished.workLeft <= -5000f)
                {
                    continue;
                }

                float workLeft = unfinished.workLeft;
                if (workLeft <= 0f)
                {
                    continue;
                }

                List<WorkGiverDef> workGivers = ResolveWorkGiversForUnfinished(unfinished);
                if (workGivers.Count == 0)
                {
                    continue;
                }

                targets.Add(new ScannedMapTarget(
                    "uft:" + unfinished.thingIDNumber,
                    workLeft,
                    workGivers,
                    workGivers.SelectMany(WorkGroupKeyResolver.ResolveGroupKeysForWorkGiver).Distinct().ToList()));
            }
        }

        private static List<WorkGiverDef> ResolveWorkGiversForBillGiver(Thing billGiverThing, Bill bill)
        {
            List<WorkGiverDef> result = new List<WorkGiverDef>();
            foreach (WorkGiverDef wg in DefDatabase<WorkGiverDef>.AllDefsListForReading)
            {
                if (wg.fixedBillGiverDefs != null && wg.fixedBillGiverDefs.Contains(billGiverThing.def))
                {
                    result.Add(wg);
                }
            }

            if (result.Count == 0 && bill.recipe?.workSkill != null)
            {
                WorkTypeDef workType = WorkTypeForSkill(bill.recipe.workSkill);
                if (workType != null)
                {
                    result.AddRange(PrimaryWorkGiversForWorkType(workType));
                }
            }

            return result.Distinct().ToList();
        }

        private static List<WorkGiverDef> ResolveWorkGiversForUnfinished(UnfinishedThing unfinished)
        {
            if (unfinished.BoundBill != null)
            {
                return new List<WorkGiverDef>();
            }

            if (unfinished.Recipe?.workSkill != null)
            {
                WorkTypeDef workType = WorkTypeForSkill(unfinished.Recipe.workSkill);
                if (workType != null)
                {
                    return PrimaryWorkGiversForWorkType(workType);
                }
            }

            return new List<WorkGiverDef>();
        }

        private static WorkTypeDef WorkTypeForSkill(SkillDef skill)
        {
            if (skill == null)
            {
                return null;
            }

            foreach (WorkTypeDef workType in DefDatabase<WorkTypeDef>.AllDefsListForReading)
            {
                if (workType.relevantSkills != null && workType.relevantSkills.Contains(skill))
                {
                    return workType;
                }
            }

            return null;
        }

        private static List<WorkGiverDef> PrimaryWorkGiversForWorkType(WorkTypeDef workType)
        {
            if (workType?.workGiversByPriority == null || workType.workGiversByPriority.Count == 0)
            {
                return new List<WorkGiverDef>();
            }

            return new List<WorkGiverDef> { workType.workGiversByPriority[0] };
        }

        private readonly struct ScannedMapTarget
        {
            public readonly string DedupeKey;
            public readonly float WorkLeft;
            public readonly List<string> WorkGiverDefNames;
            public readonly List<string> GroupKeys;

            public ScannedMapTarget(string dedupeKey, float workLeft, List<WorkGiverDef> workGivers, List<string> groupKeys)
            {
                DedupeKey = dedupeKey;
                WorkLeft = workLeft;
                WorkGiverDefNames = workGivers.Select(wg => wg.defName).Distinct().ToList();
                GroupKeys = groupKeys.Distinct().ToList();
            }
        }
    }
}
