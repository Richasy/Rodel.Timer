# Rodel.Timer

The Windows application host. It uses the Sprout Windows App SDK template shape without WinUI, owns the preserved MSIX
Store identity, and supplies the Windows-only adapter that can read the archived UWP application's LocalState and
LocalSettings.

`MainView` owns the live product state and composes the responsive Fluent shell: a central animated timer, folder and
history panes, CRUD dialogs, settings, update notes, and compact overlay. The host also applies the persisted language
and theme, coordinates save-on-close, and exposes stable UI Automation surfaces for keyboard and assistive-technology
use.
