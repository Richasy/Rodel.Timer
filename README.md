# Rodel Timer

Rodel Timer is the Sprout-based successor to the archived
[Work-Timer](https://github.com/Richasy/Work-Timer). It keeps the original forward-timer workflow and Store identity
while replacing the UWP UI and custom style layer with Sprout controls and Fluent Design.

The first development phase deliberately isolates the non-UI foundation:

- deterministic, monotonic timing with no pause state;
- folders and history with explicit invariants;
- daily duration totals and persisted preferences;
- NativeAOT-safe, versioned JSON written with atomic replacement;
- one-time import of the original `FolderList.json`, `HistoryList.json`, and `WorkTimer` local settings.

## Repository layout

```text
src/
  Rodel.Timer/       Sprout app shell, MSIX identity, and Windows legacy-data adapter
  Rodel.Timer.Core/  UI-independent domain, timing, persistence, and migration
tests/
  Rodel.Timer.Core.Tests/
```

## Build

The current bootstrap consumes a uniquely versioned local Sprout package from a sibling `Sprout` checkout.

```pwsh
dotnet build Rodel.Timer.slnx
dotnet test Rodel.Timer.slnx
dotnet publish src\Rodel.Timer\Rodel.Timer.csproj -c Release -r win-x64
```

The package manifest retains the existing Store identity
`60520B029E250.WorkingTimer`; a release package therefore upgrades the existing listing rather than creating a new
application identity.
