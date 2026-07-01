using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace WorkMonitor
{
    public static class WorkMonitorUtility
    {
        public static IEnumerable<Pawn> MonitorColonists()
        {
            Map map = Find.CurrentMap;
            if (map == null)
            {
                yield break;
            }

            foreach (Pawn pawn in map.mapPawns.FreeColonists)
            {
                if (pawn.Spawned && !pawn.DevelopmentalStage.Baby())
                {
                    yield return pawn;
                }
            }
        }

        public static string FormatDuration(int ticks, bool asHours)
        {
            if (asHours)
            {
                float hours = ticks / (float)WorkMonitorSettings.TicksPerHour;
                return "WorkMonitor.Hours".Translate(hours.ToString("0.#"));
            }

            return ticks.ToString();
        }

        public static int CurrentTicksGame()
        {
            return Find.TickManager.TicksGame;
        }

        public static int CurrentHourIndex()
        {
            return CurrentTicksGame() / WorkMonitorSettings.TicksPerHour;
        }

        public static string FormatWorkUnits(float units)
        {
            if (units >= 10000f)
            {
                return (units / 1000f).ToString("0.#") + "k";
            }

            return units.ToString("0");
        }

        public static string FormatSampleAge(int sampleTick)
        {
            if (sampleTick <= 0)
            {
                return "—";
            }

            int ageTicks = CurrentTicksGame() - sampleTick;
            if (ageTicks < WorkMonitorSettings.TicksPerHour)
            {
                return "<1h";
            }

            float hours = ageTicks / (float)WorkMonitorSettings.TicksPerHour;
            return hours.ToString("0.#") + "h";
        }

        public static string FormatGameDateTime(int sampleTick)
        {
            if (sampleTick <= 0 || Find.TickManager == null)
            {
                return "—";
            }

            int ageTicks = CurrentTicksGame() - sampleTick;
            long absTick = Find.TickManager.TicksAbs - ageTicks;
            Map map = Find.CurrentMap ?? Find.AnyPlayerHomeMap;
            if (map == null)
            {
                return GenDate.DateFullStringAt(absTick, Vector2.zero);
            }

            Vector2 location = Find.WorldGrid.LongLatOf(map.Tile);
            return GenDate.DateFullStringAt(absTick, location);
        }

        public static Vector2 MapLongitude()
        {
            Map map = Find.CurrentMap ?? Find.AnyPlayerHomeMap;
            if (map == null)
            {
                return Vector2.zero;
            }

            return Find.WorldGrid.LongLatOf(map.Tile);
        }

        public static int DayRolloverHour()
        {
            return WorkMonitorMod.Settings?.dayRolloverHour ?? 5;
        }

        public static int GetWorkDayId(long absTick, Vector2 longitude, int rolloverHour)
        {
            float lon = longitude.x;
            long rolloverOffset = rolloverHour * GenDate.TicksPerHour;
            long adjusted = absTick - rolloverOffset;
            int year = GenDate.Year(adjusted, lon);
            int day = GenDate.DayOfYear(adjusted, lon);
            return year * 1000 + day;
        }

        public static int CurrentWorkDayId()
        {
            if (Find.TickManager == null)
            {
                return 0;
            }

            return GetWorkDayId(Find.TickManager.TicksAbs, MapLongitude(), DayRolloverHour());
        }

        public static int GetWorkDayIdForHourIndex(int hourIndex)
        {
            if (Find.TickManager == null)
            {
                return 0;
            }

            int hoursAgo = CurrentHourIndex() - hourIndex;
            long absTick = Find.TickManager.TicksAbs - (long)hoursAgo * WorkMonitorSettings.TicksPerHour;
            return GetWorkDayId(absTick, MapLongitude(), DayRolloverHour());
        }

        public static int HourIndexForDayStart(int workDayId, int rolloverHour)
        {
            int year = workDayId / 1000;
            int dayOfYear = workDayId % 1000;
            float lon = MapLongitude().x;
            long absTick = (long)(year - 1) * GenDate.TicksPerYear + (dayOfYear - 1) * GenDate.TicksPerDay;
            absTick += rolloverHour * GenDate.TicksPerHour;
            return (int)(absTick / WorkMonitorSettings.TicksPerHour);
        }
    }
}
