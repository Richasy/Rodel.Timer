# Rodel.Timer.Presentation

The testable Sprout-facing application layer. It contains reactive state, presentation catalogs, and UI-independent
coordination over `Rodel.Timer.Core`; the Windows executable remains responsible for windows, activation, dialogs, and
platform adapters.

## `TimerAppStore`

`TimerAppStore` is the `[Store]` that owns all runtime timer state and orchestrates workspace persistence. It is
constructed from narrow delegates rather than concrete services, so it is fully headless-testable:

- `Func<CancellationToken, Task<TimerBootstrapResult>> loadWorkspace` and
  `Func<TimerWorkspace, CancellationToken, Task> saveWorkspace` (production passes the bootstrapper / repository methods),
- an explicit `TimeProvider`, `TimeZoneInfo`, and `IScheduler` (required — no ambient dispatcher default, keeping the
  store deterministic), plus an optional id factory and save debounce wait / max-wait.

Key design points:

- **No `_workspace` field.** `Bootstrap.Value.Workspace` is the single aggregate reference; the private `Workspace`
  getter reads it from the `Bootstrap` signal (throwing before load). Success commit and workspace visibility are the
  same signal write, so there is no race.
- **`Revision` + snapshot cache.** Every mutation bumps `Revision` and arms the debounced save. Because `TimerWorkspace`
  mutates its collections in place (the `ReadOnlyCollection` reference never changes), the store rebuilds `Folders`,
  `Entries`, and `SelectedEntries` arrays once per `(workspace, Revision)` pair — repeated getters at the same revision
  return the identical snapshot, and every mutation yields a fresh reference so inline `ItemsRepeater`s reflow.
- **Timer parity.** No pause. `StartTimer` only starts the session; `StopTimer(title)` reads
  `Workspace.SelectedFolderId` at the stop instant (so a folder switch mid-timing routes the entry to the current
  folder), sets `IsRunning = false` before adding the entry, and rejects a blank title. `Tick` advances only while
  running; `TodayTotal` reflects completed history only.
- **Reliable, retryable close.** `CloseAsync` (home-thread) flushes pending saves, forces a retry if a prior save
  failed, and rethrows on failure while re-opening the store so the next `CloseAsync` genuinely retries; concurrent
  calls observe the same task.

See `../../tests/Rodel.Timer.Tests` for the deterministic, wall-clock-free test suite that pins this behavior.
