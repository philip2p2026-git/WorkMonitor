# WorkMonitor 2-day background performance test

This runbook compares **wall-clock and save impact** with WorkMonitor disabled vs enabled (performance logging on). Use it before a release when you need confidence that background simulation overhead is acceptable.

**Scenario:** monitor UI **closed** for the entire run. Perf CSV still records `ui_active=0` when the window is closed.

2 in-game days = **48 hours** = **120,000 ticks** (2500 ticks/hour).

## What is being measured (background only)

With the monitor closed, WorkMonitor still runs via Harmony patches and two `GameComponent`s:

| Path | Frequency | Notes |
|------|-----------|--------|
| Job driver tick hook | Per colonist per tick while a job runs | Highest call volume; bill jobs add extra work |
| Job start/end hooks | On job changes | Bursty bucket updates |
| `prune_stale` | Once per in-game hour | Grows with colonist and work-giver history |
| Map snapshot (`map_sample_total` + `map_provider:*`) | Every `mapSampleIntervalHours` (default 1 h) | Often the largest spikes on big maps |

UI build/chart cost is **out of scope** for this protocol.

## Prepare two saves from the same point

1. Load your colony save at a fixed moment (note `TicksGame` and the in-game date).
2. **Save A** — duplicate the save file for the **without-mod** run.
3. **Save B** — duplicate for the **with-mod** run.

Keep constant: game speed (recommend **×3** for both), same map, no other mod changes, monitor UI **closed** for the whole run. Document which companion mods stay enabled (e.g. Work Tab) so both arms match.

## Run 1 — without WorkMonitor

1. Disable WorkMonitor (and dependencies only if RimWorld requires it for a fair test).
2. Load **Save A** from the same start tick.
3. Advance exactly **48 in-game hours** (2 RimWorld days).
4. Record externally (spreadsheet is enough):
   - Wall-clock minutes to complete
   - RimWorld debug **FPS** if visible (optional)
   - Final save file size

No in-mod CSV on this arm — this is your baseline.

## Run 2 — with WorkMonitor + performance logging

1. Enable WorkMonitor. In mod settings → **Diagnostics**, turn on **performance logging**.
2. Click **Reset perf session** (starts a new CSV).
3. Load **Save B** from the same start tick.
4. Same speed; advance **48 in-game hours**; keep the monitor closed.
5. Mod settings → **Export perf log** (or **Open perf folder**). Archive the CSV with your external notes.
6. Record the same external metrics as Run 1.

## Log file location

CSV files are written under RimWorld save data:

`SaveData/WorkMonitor/Perf/{Colony}_{Map}_{SessionStartTicks}.csv`

On Windows this is typically under:

`%USERPROFILE%\AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\`

Use **Open perf folder** in mod settings if the path differs on your install.

## CSV schema

One row per category per flush (default: hourly):

```
session_id,colony,map,hour_index,game_tick,realtime_sec,category,call_count,total_us,avg_us,counter_value,notes
```

- **session_id** — `TicksGame` at perf session start
- **realtime_sec** — `Time.realtimeSinceStartup` at flush (wall-clock anchor between runs)
- **counter_value** — population/snapshot counters; `0` on pure timing rows
- **notes** — e.g. `targets=842` on `map_sample_total`

### Timed categories

| Category | Meaning |
|----------|---------|
| `job_driver_tick` | All colonist job driver tick hook time |
| `bill_work_left` | Bill-job subset inside driver tick |
| `job_start_end` | Job start/end patch time |
| `prune_stale` | Hourly prune and rollup |
| `map_sample_total` | Full map backlog snapshot |
| `map_provider:{Name}` | Per-provider cost inside snapshot |

### Counter rows (no timing)

`active_jobs`, `group_buffer_count`, `pawn_wg_buffer_count`, `map_history_count`, `colonist_count`, `map_sample_interval_hours`, `ui_active`

## How to read the CSV

After Run 2, sum `total_us` per `category` over all rows (or pivot in Excel/LibreOffice):

| Question | What to use |
|----------|-------------|
| Steady per-tick tax | `job_driver_tick`: `total_us` ÷ `call_count` → `avg_us`; relate to ticks × active drivers |
| Bill-job extra cost | Share of `bill_work_left` vs `job_driver_tick` |
| Hourly spikes | `map_sample_total` and `map_provider:*` — find the heaviest scanner |
| History maintenance | `prune_stale` `total_us` per hour |
| Rough mod CPU budget | Sum all `total_us` ÷ delta of `realtime_sec` across the run (order-of-magnitude % of one core) |
| Save bloat | Compare save file sizes Run 1 vs Run 2 |

Compare Run 1 vs Run 2 **wall-clock minutes** for the primary user-visible result; use the CSV to see *where* time went inside the mod.

## Acceptance criteria (practical guidance)

- **Background overhead < ~1% wall-clock** vs without-mod on a typical mid-size colony → reasonable for release.
- If **`map_sample_total`** dominates, raising **map sample interval** to 3–6 in-game hours (existing mod setting) reduces spikes without code changes.
- If save size grows noticeably in Run 2, investigate retention settings and colony size before shipping.

Archive both external notes and the Run 2 CSV with the build/version you tested.

## Recorded results

See [PerfTestResults.md](PerfTestResults.md) for the **test Wmonitor** colony run (卡斯托爾維加, ~48 in-game hours, Jan 2026).
