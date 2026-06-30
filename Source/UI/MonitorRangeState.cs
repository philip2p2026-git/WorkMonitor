using System.Collections.Generic;
using UnityEngine;
using Verse;
using WorkMonitor;

namespace WorkMonitor.UI
{
    public enum MonitorRangePreset
    {
        Hours6,
        Hours12,
        Hours24,
        Hours48,
        Days7,
        Days14,
        Quadrums4,
        Quadrums8,
        Years3,
        Years5
    }

    public class MonitorRangeState
    {
        private static readonly MonitorRangePreset[] AllPresetsList =
        {
            MonitorRangePreset.Hours6,
            MonitorRangePreset.Hours12,
            MonitorRangePreset.Hours24,
            MonitorRangePreset.Hours48,
            MonitorRangePreset.Days7,
            MonitorRangePreset.Days14,
            MonitorRangePreset.Quadrums4,
            MonitorRangePreset.Quadrums8,
            MonitorRangePreset.Years3,
            MonitorRangePreset.Years5
        };

        public MonitorRangePreset Preset { get; private set; }
        public int SpanHours { get; private set; }
        public int RangeHours => SpanHours;
        public string Label { get; private set; }

        public MonitorRangeState()
        {
            SetPreset(WorkMonitorMod.Settings?.DefaultRangePreset ?? MonitorRangePreset.Hours24);
        }

        public int MinHourIndex => WorkMonitorUtility.CurrentHourIndex() - SpanHours;

        public bool UsesHourlyChart => SpanHours <= 48;
        public bool UsesDailyChart => SpanHours > 48 && SpanHours <= 336;

        public static IReadOnlyList<MonitorRangePreset> AllPresets => AllPresetsList;

        public void SetPreset(MonitorRangePreset preset)
        {
            Preset = preset;
            SpanHours = PresetToSpanHours(preset);
            Label = PresetToLabel(preset);
        }

        public static int PresetToSpanHours(MonitorRangePreset preset)
        {
            return preset switch
            {
                MonitorRangePreset.Hours6 => 6,
                MonitorRangePreset.Hours12 => 12,
                MonitorRangePreset.Hours24 => 24,
                MonitorRangePreset.Hours48 => 48,
                MonitorRangePreset.Days7 => 168,
                MonitorRangePreset.Days14 => 336,
                MonitorRangePreset.Quadrums4 => 1440,
                MonitorRangePreset.Quadrums8 => 2880,
                MonitorRangePreset.Years3 => 4320,
                MonitorRangePreset.Years5 => 7200,
                _ => 24
            };
        }

        public static MonitorRangePreset PresetAtIndex(int index)
        {
            int clamped = Mathf.Clamp(index, 0, AllPresetsList.Length - 1);
            return AllPresetsList[clamped];
        }

        public static int IndexOfPreset(MonitorRangePreset preset)
        {
            for (int i = 0; i < AllPresetsList.Length; i++)
            {
                if (AllPresetsList[i] == preset)
                {
                    return i;
                }
            }

            return 2;
        }

        public static string PresetToLabel(MonitorRangePreset preset)
        {
            return preset switch
            {
                MonitorRangePreset.Hours6 => "WorkMonitor.Range6h".Translate(),
                MonitorRangePreset.Hours12 => "WorkMonitor.Range12h".Translate(),
                MonitorRangePreset.Hours24 => "WorkMonitor.Range24h".Translate(),
                MonitorRangePreset.Hours48 => "WorkMonitor.Range48h".Translate(),
                MonitorRangePreset.Days7 => "WorkMonitor.Range7d".Translate(),
                MonitorRangePreset.Days14 => "WorkMonitor.Range14d".Translate(),
                MonitorRangePreset.Quadrums4 => "WorkMonitor.Range4Quadrum".Translate(),
                MonitorRangePreset.Quadrums8 => "WorkMonitor.Range8Quadrum".Translate(),
                MonitorRangePreset.Years3 => "WorkMonitor.Range3Year".Translate(),
                MonitorRangePreset.Years5 => "WorkMonitor.Range5Year".Translate(),
                _ => "WorkMonitor.Range24h".Translate()
            };
        }

        public static MonitorRangePreset MapSettingsToPreset(MonitorRangePreset settingsPreset)
        {
            return settingsPreset;
        }

        public static MonitorRangePreset MapHoursToPreset(int hours)
        {
            return hours switch
            {
                <= 6 => MonitorRangePreset.Hours6,
                <= 12 => MonitorRangePreset.Hours12,
                <= 24 => MonitorRangePreset.Hours24,
                _ => MonitorRangePreset.Hours48
            };
        }
    }
}
