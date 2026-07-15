using Rodel.Timer.Core.Folders;

using Sprout.Controls;

namespace Rodel.Timer.Presentation;

/// <summary>A single folder icon choice: the persisted <see cref="TimerFolderIcon"/> and the Fluent glyph it paints as.</summary>
/// <param name="Icon">The persisted folder icon value.</param>
/// <param name="Symbol">The Fluent symbol the icon resolves to.</param>
public readonly record struct FolderIconOption(
    TimerFolderIcon Icon,
    FluentSymbol Symbol);

/// <summary>
/// One icon-picker cell's fully resolved choice: the persisted icon, its Fluent glyph, whether it is the currently
/// selected icon, and a monotonic UI revision. The picker builds a fresh list of these each render and feeds it to a
/// stable, same-key <c>ItemsRepeater</c> (keyed by <see cref="TimerFolderIcon"/>), so selecting a new icon rebinds only
/// the two changed cells in place — the grid, its scroll position, and keyboard focus are preserved (no whole-grid
/// remount). The <see cref="Revision"/> carries the owning view's theme/language stamp into the record's value equality,
/// so a theme or language switch (which changes no icon or selection flag) still yields a <b>new</b> value at each
/// unchanged icon key, forcing the same-key rebind to rebuild the cell with live colours and a re-localized name.
/// </summary>
/// <param name="Icon">The persisted folder icon value.</param>
/// <param name="Symbol">The Fluent symbol the icon resolves to.</param>
/// <param name="IsSelected">Whether this icon is the currently selected one (drives the accent highlight).</param>
/// <param name="Revision">The owning view's theme/language stamp, folded into value equality so a stamp bump re-values every cell.</param>
public readonly record struct IconChoice(
    TimerFolderIcon Icon,
    FluentSymbol Symbol,
    bool IsSelected,
    int Revision);

/// <summary>Maps the 40 <see cref="TimerFolderIcon"/> values to their Fluent glyphs for the icon picker and folder rows.</summary>
public static class FolderIconCatalog
{
    /// <summary>All 40 folder icon choices, in picker order.</summary>
    public static IReadOnlyList<FolderIconOption> All { get; } =
    [
        new(TimerFolderIcon.Activity, FluentSymbol.BroadActivityFeed),
        new(TimerFolderIcon.Airplay, FluentSymbol.Cast),
        new(TimerFolderIcon.AlertTriangle, FluentSymbol.AlertUrgent),
        new(TimerFolderIcon.Anchor, FluentSymbol.Navigation),
        new(TimerFolderIcon.Aperture, FluentSymbol.CameraSparkles),
        new(TimerFolderIcon.Archive, FluentSymbol.Archive),
        new(TimerFolderIcon.Award, FluentSymbol.Trophy),
        new(TimerFolderIcon.BarChart, FluentSymbol.DataBarVertical),
        new(TimerFolderIcon.BarChart2, FluentSymbol.DataBarHorizontal),
        new(TimerFolderIcon.Battery, FluentSymbol.Battery10),
        new(TimerFolderIcon.Bluetooth, FluentSymbol.Bluetooth),
        new(TimerFolderIcon.Book, FluentSymbol.Book),
        new(TimerFolderIcon.Bookmark, FluentSymbol.Bookmark),
        new(TimerFolderIcon.Box, FluentSymbol.Box),
        new(TimerFolderIcon.Briefcase, FluentSymbol.Briefcase),
        new(TimerFolderIcon.Calendar, FluentSymbol.Calendar),
        new(TimerFolderIcon.Camera, FluentSymbol.Camera),
        new(TimerFolderIcon.Cast, FluentSymbol.Cast),
        new(TimerFolderIcon.Chrome, FluentSymbol.Globe),
        new(TimerFolderIcon.Clipboard, FluentSymbol.Clipboard),
        new(TimerFolderIcon.Clock, FluentSymbol.Clock),
        new(TimerFolderIcon.Cloud, FluentSymbol.Cloud),
        new(TimerFolderIcon.Code, FluentSymbol.Code),
        new(TimerFolderIcon.Coffee, FluentSymbol.DrinkCoffee),
        new(TimerFolderIcon.Cpu, FluentSymbol.DeveloperBoard),
        new(TimerFolderIcon.CreditCard, FluentSymbol.WalletCreditCard),
        new(TimerFolderIcon.Disc, FluentSymbol.Record),
        new(TimerFolderIcon.Edit, FluentSymbol.Edit),
        new(TimerFolderIcon.Eye, FluentSymbol.Eye),
        new(TimerFolderIcon.Facebook, FluentSymbol.PeopleCommunity),
        new(TimerFolderIcon.Feather, FluentSymbol.LeafOne),
        new(TimerFolderIcon.Figma, FluentSymbol.DesignIdeas),
        new(TimerFolderIcon.File, FluentSymbol.Document),
        new(TimerFolderIcon.Folder, FluentSymbol.Folder),
        new(TimerFolderIcon.Framer, FluentSymbol.Frame),
        new(TimerFolderIcon.Gift, FluentSymbol.Gift),
        new(TimerFolderIcon.Home, FluentSymbol.Home),
        new(TimerFolderIcon.Image, FluentSymbol.Image),
        new(TimerFolderIcon.Inbox, FluentSymbol.MailInbox),
        new(TimerFolderIcon.Info, FluentSymbol.Info),
    ];

    /// <summary>Resolves a persisted folder icon to the Fluent glyph it paints as.</summary>
    /// <param name="icon">The persisted folder icon value.</param>
    /// <returns>The Fluent symbol for <paramref name="icon"/>.</returns>
    public static FluentSymbol Resolve(TimerFolderIcon icon)
    {
        foreach (FolderIconOption option in All)
        {
            if (option.Icon == icon)
            {
                return option.Symbol;
            }
        }

        return FluentSymbol.BroadActivityFeed;
    }

    /// <summary>
    /// Builds the icon-picker choice list for a selected icon — every catalog option in picker order, each flagged with
    /// whether it is <paramref name="selected"/> and stamped with <paramref name="revision"/>. Exactly one choice is
    /// selected when <paramref name="selected"/> is a catalog value; none is selected otherwise. Pure and
    /// allocation-explicit so the picker can rebuild it each render; the <paramref name="revision"/> is folded into every
    /// choice so a theme/language stamp bump re-values each unchanged cell, driving the same-key rebind to re-resolve
    /// live colours and re-localized names.
    /// </summary>
    /// <param name="selected">The currently selected icon.</param>
    /// <param name="revision">The owning view's theme/language stamp, stamped into every choice.</param>
    /// <returns>The 40 icon choices, in picker order, with the selected one flagged.</returns>
    public static IReadOnlyList<IconChoice> Choices(TimerFolderIcon selected, int revision)
    {
        var choices = new List<IconChoice>(All.Count);
        foreach (FolderIconOption option in All)
        {
            choices.Add(new IconChoice(option.Icon, option.Symbol, option.Icon == selected, revision));
        }

        return choices;
    }
}
