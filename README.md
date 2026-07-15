# Rodel Timer

Rodel Timer is the Sprout-based successor to the archived
[Work-Timer](https://github.com/Richasy/Work-Timer). It keeps the original forward-timer workflow and Store identity
while replacing the UWP UI and custom style layer with Sprout controls and Fluent Design.

The rewrite now includes the complete product surface:

- deterministic, monotonic timing with no pause state;
- folders and history with explicit invariants;
- daily duration totals and persisted preferences;
- NativeAOT-safe, versioned JSON written with atomic replacement;
- one-time import of the original `FolderList.json`, `HistoryList.json`, and `WorkTimer` local settings;
- a responsive Fluent shell with animated folder/history panes, list reflow, and compact overlay;
- live English/Chinese localization, system/light/dark themes, system fonts, and localized update notes;
- stable UI Automation ids, accessible names and descriptions, keyboard access keys, and transactional announcements.

## Repository layout

```text
src/
  Rodel.Timer/               Sprout app shell, Fluent UI, MSIX identity, and Windows legacy-data adapter
  Rodel.Timer.Core/          UI-independent domain, timing, persistence, and migration
  Rodel.Timer.Presentation/  Reactive application state and UI-independent presentation policies
tests/
  Rodel.Timer.Core.Tests/
  Rodel.Timer.Tests/
```

## Build

The app consumes the Sprout packages configured in `Directory.Packages.props`.

```pwsh
dotnet build Rodel.Timer.slnx
dotnet test Rodel.Timer.slnx
dotnet publish src\Rodel.Timer\Rodel.Timer.csproj -c Release -r win-x64
```

The package manifest retains the existing Store identity
`60520B029E250.WorkingTimer`; a release package therefore upgrades the existing listing rather than creating a new
application identity.
