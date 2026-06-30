using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace WorkMonitor.UI
{
    public static class WorkMonitorDropdownUtility
    {
        public static bool DrawDropdown(Rect rect, string label, List<FloatMenuOption> options)
        {
            if (!Widgets.ButtonText(rect, label.Truncate(rect.width - 8f)))
            {
                return false;
            }

            if (options != null && options.Count > 0)
            {
                Find.WindowStack.Add(new FloatMenu(options));
            }

            return true;
        }

        public static List<FloatMenuOption> BuildOptions<T>(
            IEnumerable<T> items,
            Func<T, string> label,
            Action<T> onSelect)
        {
            var options = new List<FloatMenuOption>();
            foreach (T item in items)
            {
                T captured = item;
                options.Add(new FloatMenuOption(label(captured), () => onSelect(captured)));
            }

            return options;
        }

        public static bool DrawRangeDropdown(Rect rect, MonitorRangeState rangeState, System.Action onChanged)
        {
            List<FloatMenuOption> options = BuildOptions(
                MonitorRangeState.AllPresets,
                MonitorRangeState.PresetToLabel,
                preset =>
                {
                    rangeState.SetPreset(preset);
                    onChanged?.Invoke();
                });
            TooltipHandler.TipRegion(rect, "WorkMonitor.RangeLabel".Translate());
            return DrawDropdown(rect, rangeState.Label, options);
        }
    }
}
