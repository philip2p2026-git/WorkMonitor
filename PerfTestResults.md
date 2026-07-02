# WorkMonitor performance test results

Recorded from the **test Wmonitor** save family (Jan 2026). Protocol: [PerfTest.md](PerfTest.md) — background only, monitor UI closed.

## Test setup

| Item | Value |
|------|--------|
| Colony | 卡斯托爾維加 |
| Map | 殖民地 |
| Colonists | 29 at start → 26 at end |
| Map sample interval | 1 in-game hour |
| Monitor UI | Closed (`ui_active=0` entire run) |
| Perf logging | **On** (mod + observer overhead) |

### Save folder

`%USERPROFILE%\AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\Saves\test Wmonitor#§#…`

### Perf CSV files

| File | Use |
|------|-----|
| `WorkMonitor/Perf/卡斯托爾維加_殖民地_1692.csv` | **Primary** — full ~2-day logged run |
| `WorkMonitor/Perf/卡斯托爾維加_殖民地_8478.csv` | **Ignore** — counter-only rows, duplicate headers, no timing data (short session / manual export artifact) |

### Saves compared (day 2 ~7am endpoints)

| Save | Size |
|------|------|
| `test Wmonitor - no mod day2 7am` | 6.76 MB |
| `test Wmonitor - with mod no log day2 7am` | 8.44 MB |
| `test Wmonitor - with mod logged day2 7am` | 8.99 MB |

---

## Run coverage (session `1692`)

| Metric | Value |
|--------|--------|
| In-game ticks | **1692 → 122,648** (~**48.4 h**, ~2 RimWorld days) |
| In-game hours logged | **0–49** (50 flush windows) |
| Wall-clock (CSV `realtime_sec`) | **557.2 → 926.7 s** → **369.5 s** (~6.2 min at test game speed) |
| Map samples | **49** |
| Open map targets per sample | ~**1,840–1,906** (`notes=targets=…` on `map_sample_total`) |

---

## CPU cost (top-level categories only)

**Do not sum every CSV row.** Nested scopes double-count:

- `bill_work_left` is measured inside `job_driver_tick`
- `map_provider:*` is measured inside `map_sample_total`

Use these four top-level categories:

| Category | Total CPU | Notes |
|----------|-----------|--------|
| `job_driver_tick` | **2.60 s** | Steady load; **3,336,526** calls, **~0.78 µs** avg per call |
| `map_sample_total` | **1.21 s** | Hourly spikes; **~25 ms** avg/sample, peak **~49 ms** |
| `job_start_end` | **0.35 s** | Job change bursts |
| `prune_stale` | **0.09 s** | Hourly rollup; **~0.25 ms** early → **~4 ms** by hour 49 |
| **Top-level total** | **~4.25 s** | |

### Rough overhead vs wall-clock

`4.25 s ÷ 369.5 s ≈ **1.15%**` of one core during the logged run (**with profiler on**).

Fits the practical release guideline (**< 1–2%** background) for this colony size. **With logging off**, expect slightly less than this figure.

### Bill jobs (subset)

| Category | Total CPU | Calls | Avg |
|----------|-----------|-------|-----|
| `bill_work_left` | 2.25 s | 689,151 | ~3.3 µs |

Reported separately for diagnosis; already included in `job_driver_tick` totals above.

---

## Map scan breakdown (avg per hourly sample)

| Provider | ~ms / sample |
|----------|----------------|
| `ListerFilthMapWorkProvider` | **6.1** |
| `ListerRefuelMapWorkProvider` | **4.6** |
| `ListerHaulablesMapWorkProvider` | **3.0** |
| `BillMapWorkProvider` | **2.9** |
| `BrokenDownBuildingMapWorkProvider` | **1.8** |
| `CompBuildingMapWorkProvider` | **1.8** |
| `UnfinishedThingMapWorkProvider` | **1.2** |
| `SnowClearMapWorkProvider` | **0.7** |
| Others | &lt; 0.3 |

**Largest tuning knob:** raise **map sample interval** to 3–6 h (mod setting) to cut hourly spike cost roughly in proportion.

---

## History growth (counters)

| Counter | Start → end |
|---------|-------------|
| `pawn_wg_buffer_count` | 1 → **146** |
| `group_buffer_count` | 1 → **13** |
| `map_history_count` | 2 → **51** (cap 80) |
| `active_jobs` | 0 → ~7 typical mid-run |

Prune cost stayed small even after two in-game days.

---

## Save file impact

| Comparison | Delta |
|------------|--------|
| No mod → with mod (no log), day 2 7am | **+~1.68 MB** (+25%) |
| No mod → with mod (logged), day 2 7am | **+~2.23 MB** (+33%) |
| With mod no log → with mod logged | **+~0.55 MB** |

WorkMonitor adds meaningful **save size** (tracker + map snapshot history), separate from CPU. Logging adds a modest extra amount.

---

## Conclusions

1. **CPU — acceptable for release** on a ~29-colonist colony with ~1,900 map backlog targets at 1 h map sampling.
2. **Steady tax** = colonist job driver hooks; **hourly tax** = map lister scans (filth, refuel, haul, bills).
3. **Profiler** adds measurable overhead; use **with mod, logging off** for fairest wall-clock vs **no mod**.
4. **8478.csv** indicates a recorder UX issue (duplicate header, counter-only append); primary analysis should use **1692.csv** only.

### Open items

- Wall-clock minutes for **no mod day2 7am** vs **with mod no log day2 7am** were not recorded in this doc — add when available to cross-check the ~1.15% CSV estimate.

---

## Version

Results from perf logging shipped in commit `4ba3fda` (*Add opt-in CSV performance logging for background simulation overhead.*).
