# Upgrading from Work‑Timer

Working Timer is the direct successor to the archived
[Work‑Timer](https://github.com/Richasy/Work-Timer). Upgrading is **in place and data‑safe** — your folders, history
and preferences are carried over automatically the first time the new app runs.

## Why the upgrade is seamless

Both apps share the **same Microsoft Store package identity**:

| | Work‑Timer | Working Timer |
|---|---|---|
| Package name | `60520B029E250.WorkingTimer` | `60520B029E250.WorkingTimer` |
| Publisher | `CN=9EFA2047‑…` | `CN=9EFA2047‑…` |
| Version | `1.0.4.0` | `2.0.0.0` |

Because the name and publisher match, Windows treats the new package as an **upgrade** of the old one, and the app
keeps the **same local data folder** (`…\Packages\60520B029E250.WorkingTimer_*\LocalState`). The old data files are
therefore already present when the new app first starts.

## What is imported

On the **first launch only** (when no new‑format workspace exists yet), the app reads the legacy storage and converts
it:

| Legacy source | Imported into |
|---|---|
| `LocalState\FolderList.json` | Your folders (name, icon, created time) |
| `LocalState\HistoryList.json` | Your history entries (title, folder, duration, time) |
| `LocalSettings` → `WorkTimer` container | Theme, language, font, daily target, and the last‑selected folder |

The importer **repairs recoverable issues** instead of failing — an entry with a missing name is renamed, an unknown
icon falls back to a default, a history entry pointing at a deleted folder is re‑homed to your first folder, and a
negative duration is clamped to zero. Each repair is recorded as a non‑fatal warning.

## Guarantees

- **Read‑only on the legacy files.** The original `FolderList.json` / `HistoryList.json` and `WorkTimer` settings are
  never modified or deleted by the migration.
- **Atomic, versioned write.** The converted workspace is written in the new format with atomic replacement, so a
  crash mid‑write cannot corrupt it.
- **Runs once.** After the first successful import the app always loads the new workspace; it never re‑imports or
  overwrites your newer data.
- **Fail‑safe.** If the legacy data is structurally damaged, the app **shows an error surface instead of overwriting
  anything** and retries on the next launch — it never silently starts empty on top of recoverable data.

## Notes

- Migration runs only for the **packaged (MSIX)** app — i.e. the real Store/side‑load upgrade path. A loose,
  unpackaged developer build uses its own separate data location and does not import anything.
- A clean install on a new machine, or uninstalling the old app with *"also delete its data"*, leaves nothing to
  import — that is expected, not data loss.
