using System.Diagnostics.CodeAnalysis;
using System.Globalization;

using Rodel.Timer.Core.Folders;
using Rodel.Timer.Core.History;
using Rodel.Timer.Presentation;
using Rodel.Timer.Styling;

using Sprout;
using Sprout.Graphics;
using Sprout.Reconcile;
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
        string created = folder.CreatedAt.LocalDateTime.ToString("d", CultureInfo.CurrentCulture);
        return new FolderRowView(
            folder.Name,
            created,
            FolderIconCatalog.Resolve(folder.Icon),
            Brush.CreateSolid(colors.AccentTextFillColorPrimary),
            Brush.CreateSolid(colors.AccentFillColorDefault.MultiplyAlpha(0.14f)),
            colors.TextFillColorSecondary,
            new TextFormat(TextFormat.DefaultFontFamily, 13f, TextFormat.SemiBoldWeight),
            new TextFormat(TextFormat.DefaultFontFamily, 11f, TextFormat.NormalWeight));
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
        return new HistoryRowView(
            entry.Title,
            $"{duration}  \u00B7  {created}",
            colors.TextFillColorSecondary,
            new TextFormat(TextFormat.DefaultFontFamily, 13f, TextFormat.SemiBoldWeight),
            new TextFormat(TextFormat.DefaultFontFamily, 11f, TextFormat.NormalWeight),
            Brush.CreateSolid(colors.AccentTextFillColorPrimary),
            Brush.CreateSolid(colors.AccentFillColorDefault.MultiplyAlpha(0.14f)));
    }
}
