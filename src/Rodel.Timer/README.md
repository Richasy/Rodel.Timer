# Rodel.Timer

The Windows application host. It uses the Sprout Windows App SDK template shape without WinUI, owns the preserved MSIX
Store identity, and supplies the Windows-only adapter that can read the archived UWP application's LocalState and
LocalSettings.

`MainView` owns the live product state and composes the responsive Fluent shell: an open-canvas timer stage, title-bar
anchored Folders/History surfaces with row context menus, CRUD dialogs, settings, update notes, and an adaptive compact
overlay. The host also applies the persisted language, theme, font, and window-background material, coordinates
save-on-close, and exposes stable UI Automation surfaces for keyboard and assistive-technology use.
