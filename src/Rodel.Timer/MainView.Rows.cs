using System.Diagnostics.CodeAnalysis;
using System.Globalization;

using Rodel.Timer.Core.Folders;
using Rodel.Timer.Core.History;
using Rodel.Timer.Presentation;
using Rodel.Timer.Styling;

using Sprout;
using Sprout.Graphics;
using Sprout.Reconcile;
using Sprout.Semantics;
using Sprout.Theming;

namespace Rodel.Timer;

/// <summary>
/// The folder- and history-row templates. They live on <see cref="MainView"/> (a reference-type control) rather than on
/// the row-list value views so their per-row closures capture live state (selection, callbacks) without a
/// struct-<c>this</c> capture, and so the generated repeater reconciler receives a stable method-group template.
/// </summary>
public sealed partial class MainView
{
    /// <summary>Builds one folder row from the projected row data + live theme state (the folder list's row template).</summary>
    /// <param name="data">The folder row data (folder + selection flag + theme revision).</param>
    /// <returns>The resolved <see cref="FolderRowView"/> for the folder.</returns>
    [SuppressMessage(
        "Performance",
        "CA1863:Use 'CompositeFormat'",
        Justification = "The format string is a live-localized resource that changes on a language switch; caching a CompositeFormat would freeze the initial language.")]
    internal FolderRowView BuildFolderRow(FolderRowData data)
    {
        TimerChrome chrome = _chrome;
        ThemeColors colors = chrome.Colors;
        TimerFolder folder = data.Folder;
        bool selected = data.IsSelected;
        Guid id = folder.Id;
        string created = folder.CreatedAt.LocalDateTime.ToString("d", CultureInfo.CurrentCulture);
        return new FolderRowView(
            folder.Name,
            created,
            FolderIconCatalog.Resolve(folder.Icon),
            Brush.CreateSolid(colors.TextFillColorPrimary),
            Brush.CreateSolid(colors.SubtleFillColorSecondary),
            Brush.CreateSolid(selected ? colors.AccentFillColorDefault : colors.SubtleFillColorTransparent),
            Brush.CreateSolid(selected ? colors.SubtleFillColorSecondary : colors.SubtleFillColorTransparent),
            colors.TextFillColorPrimary,
            colors.TextFillColorSecondary,
            new TextFormat(FontFamily, 14f, TextFormat.SemiBoldWeight),
            new TextFormat(FontFamily, 12f, TextFormat.NormalWeight),
            TimerVisuals.Ghost(colors),
            TimerVisuals.Ghost(colors),
            () => chrome.SelectFolder(id),
            () => chrome.ShowFolderMenu(id),
            new AutomationInfo
            {
                Name = folder.Name,
                AutomationId = TimerAutomationIds.FolderSelect(id),
                ItemStatus = selected ? Resources.CurrentFolder : null,
            },
            new AutomationInfo
            {
                Name = string.Format(CultureInfo.CurrentCulture, Resources.MoreActions, folder.Name),
                AutomationId = TimerAutomationIds.FolderMore(id),
            });
    }

    /// <summary>Builds one history card from the projected row data + live theme state (the history list's row template).</summary>
    /// <param name="data">The history row data (entry + theme revision).</param>
    /// <returns>The resolved <see cref="HistoryRowView"/> for the entry.</returns>
    [SuppressMessage(
        "Performance",
        "CA1863:Use 'CompositeFormat'",
        Justification = "The format string is a live-localized resource that changes on a language switch; caching a CompositeFormat would freeze the initial language.")]
    internal HistoryRowView BuildHistoryRow(HistoryRowData data)
    {
        TimerChrome chrome = _chrome;
        ThemeColors colors = chrome.Colors;
        TimerEntry entry = data.Entry;
        string duration = TimerFormat.Clock(TimeSpan.FromSeconds(entry.DurationSeconds));
        string created = entry.CreatedAt.LocalDateTime.ToString("g", CultureInfo.CurrentCulture);
        Guid id = entry.Id;
        string accessibleDescription = string.Format(
            CultureInfo.CurrentCulture,
            Resources.HistoryItemAccessibleDescription,
            entry.Title,
            duration,
            created);
        return new HistoryRowView(
            entry.Title,
            $"{duration}  \u00B7  {created}",
            colors.TextFillColorPrimary,
            colors.TextFillColorSecondary,
            new TextFormat(FontFamily, 14f, TextFormat.SemiBoldWeight),
            new TextFormat(FontFamily, 12f, TextFormat.NormalWeight),
            Brush.CreateSolid(colors.TextFillColorSecondary),
            Brush.CreateSolid(colors.SubtleFillColorSecondary),
            TimerVisuals.Ghost(colors),
            () => chrome.ShowHistoryMenu(id),
            new AutomationInfo
            {
                Name = string.Format(CultureInfo.CurrentCulture, Resources.MoreActions, entry.Title),
                HelpText = accessibleDescription,
                FullDescription = accessibleDescription,
                AutomationId = TimerAutomationIds.HistoryMore(id),
            });
    }
}
