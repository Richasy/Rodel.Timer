# Rodel.Timer.Tests

Headless tests for the Sprout application layer: generated localization accessors, presentation catalogs, reactive
state, accessibility semantics, and view reconciliation. The suite does not launch or install the application.

## `TimerAppStore` suite (deterministic, zero wall-clock)

The `Presentation/TimerAppStore*Tests.cs` files exercise the store with no real time or threads. `TestSupport` provides
the deterministic machinery so every test advances the world by hand:

- **`QueueingScheduler`** — an `IScheduler` whose `Post` *enqueues* commits (`DrainOnce` / `DrainUntilIdle` run them) and
  whose `Advance` moves a manual clock to fire scheduled debounce callbacks. This cleanly separates "async commit"
  (drain) from "debounce timer" (advance).
- **`ManualTimeProvider`** — an independent wall clock + monotonic timestamp source advanced explicitly.
- **`TimerStoreHarness`** — wires the store to fully controllable load / save delegates (block, fault-once, capture
  saved workspaces) and a deterministic id factory.

Coverage spans bootstrap phases (idle → loading → success / error / cancel / latest-wins, committing only on drain),
snapshot reference-equality and `Effect` re-run, timer start/stop/tick/monotonicity and stop-moment folder capture,
every mutation bumping `Revision` by exactly one with the right invariants / cascade / preferences behavior, debounce
trailing / max-wait / sequential non-overlap / failure-recovery, and close / dispose reliability (flush, prior-error
retry, failure-resumes-mutations, concurrent-close dedup, disposed methods throwing). No test uses `Task.Delay`,
timeouts, or wall-clock waits.
