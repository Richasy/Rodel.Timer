using Rodel.Timer.Core.Folders;
using Rodel.Timer.Core.Settings;
using Rodel.Timer.Presentation;
using Rodel.Timer.Styling;

using Sprout;
using Sprout.Controls;
using Sprout.Graphics;
using Sprout.Layout;
using Sprout.Reconcile;
using Sprout.Semantics;
using Sprout.Theming;
using Sprout.Widgets;

using static Sprout.Markup;

namespace Rodel.Timer.Dialogs;

/// <summary>
/// One icon cell of the folder icon picker — a fixed-size glyph button that highlights (accent palette + text-on-accent
/// glyph) when it is the selected icon. It is a multi-part composite so each cell projects its own interaction and
/// accessibility nodes; the host supplies the resolved palette, glyph brush, pick callback, and accessibility info.
/// </summary>
/// <param name="Symbol">The Fluent glyph the cell paints.</param>
/// <param name="Palette">The resolved button palette (accent when selected).</param>
/// <param name="IconBrush">The resolved glyph brush.</param>
/// <param name="OnPick">Selects this icon.</param>
/// <param name="Info">The cell's accessibility info (name + id).</param>
[Composite]
public readonly partial record struct IconCellView(
    FluentSymbol Symbol,
    ButtonPalette Palette,
    Brush IconBrush,
    Action OnPick,
    AutomationInfo Info)
{
    /// <summary>A square glyph button sized for the picker grid, centred in its (stretched) grid cell.</summary>
    public Ui Body => Overlay(
        Button(
            Icon.Fluent(Symbol, FluentIconVariant.Regular, TimerVisuals.IconSize.Medium, IconBrush),
            OnPick,
            Palette)
            .Style(TimerVisuals.Buttons.Icon)
            .Width(44f)
            .Height(44f)
            .HAlign(HorizontalAlignment.Center)
            .Automation(Info));
}

/// <summary>
/// The scrolling, uniform-grid icon picker of the folder editor — a <c>[Composite]</c> value view hosting the
/// 40-glyph repeater. The host re-supplies the <see cref="Choices"/> list (rebuilt from the current selection and the
/// theme/language revision) on every render, so the stable, icon-keyed repeater rebinds only the cells whose value
/// actually changed — the grid identity, scroll position, and keyboard focus are preserved (no whole-grid remount).
/// </summary>
/// <param name="Choices">The icon choices to render, each flagged with whether it is the selected icon.</param>
/// <param name="Template">The cell template resolving each choice to a highlighted <see cref="IconCellView"/>.</param>
[Composite]
public readonly partial record struct IconPickerView(
    IReadOnlyList<IconChoice> Choices,
    Func<IconChoice, IconCellView> Template)
{
    /// <summary>A vertically scrolling uniform grid of icon cells.</summary>
    public Ui Body => Overlay(
        ScrollView(
            ItemsRepeater(
                Choices,
                static c => c.Icon,
                Template,
                new UniformGridLayout
                {
                    ItemWidth = 44f,
                    ItemHeight = 44f,
                    ColumnSpacing = 8f,
                    RowSpacing = 8f,
                    ItemsStretch = UniformGridItemsStretch.Fill,
                }))
            .Vertical(ScrollMode.Auto)
            .HorizontalScrollBar(ScrollBarVisibility.Hidden)
            .Height(260f));
}

/// <summary>
/// The live content of the add/edit-folder dialog — a name text box and a 40-glyph icon picker. It owns the edited name
/// and selected icon as reactive state so the picker highlight follows selection and the host can read the final values
/// on commit. Selecting an icon rebuilds the picker's <see cref="IconChoice"/> list, so the icon-keyed repeater rebinds
/// only the two changed cells in place — the grid, its scroll position, and keyboard focus are preserved.
/// </summary>
public sealed partial class FolderEditContent : Control
{
    private readonly MainView _main;

    /// <summary>Creates the folder editor seeded with an initial name and icon.</summary>
    /// <param name="main">The state-owning root view (the source of the live theme colours and language stamp).</param>
    /// <param name="initialName">The initial folder name (empty when adding).</param>
    /// <param name="initialIcon">The initially selected icon.</param>
    /// <exception cref="ArgumentNullException"><paramref name="main"/> is <see langword="null"/>.</exception>
    public FolderEditContent(MainView main, string initialName, TimerFolderIcon initialIcon)
    {
        _main = main ?? throw new ArgumentNullException(nameof(main));
        FolderName = initialName ?? string.Empty;
        SelectedIcon = initialIcon;
    }

    /// <summary>The edited folder name (read by the host on commit).</summary>
    public partial string FolderName { get; private set; }

    /// <summary>The selected folder icon (read by the host on commit).</summary>
    public partial TimerFolderIcon SelectedIcon { get; private set; }

    /// <summary>Raised (with the new name) whenever the edited name changes, so the host can enable/disable its commit button.</summary>
    public event Action<string>? NameChanged;

    /// <summary>
    /// The live theme colours resolved from the owning view's current theme; reading <see cref="MainView.ThemeStamp"/>
    /// subscribes this dialog's body so an in-place theme or language switch re-renders it with the live palette.
    /// </summary>
    private ThemeColors Colors
    {
        get
        {
            _ = _main.ThemeStamp;
            return _main.Window.Theme.Colors;
        }
    }

    /// <inheritdoc/>
    public Ui Body => Overlay(
        Stack(
            TextBox()
                .Text(FolderName)
                .OnTextChanged(SetName)
                .Placeholder(Resources.FolderNamePlaceholder)
                .Automation(new AutomationInfo
                {
                    Name = Resources.FolderName,
                    AutomationId = "RodelTimer.FolderEdit.Name",
                }),
            Text(
                Resources.ChooseIcon,
                new TextFormat(TextFormat.DefaultFontFamily, 13f, TextFormat.NormalWeight),
                Colors.TextFillColorSecondary),
            new IconPickerView(FolderIconCatalog.Choices(SelectedIcon, _main.ThemeStamp), BuildCell))
            .Spacing(TimerVisuals.Spacing.Medium)
            .Width(TimerVisuals.MaxWidth.FolderDialog));

    /// <summary>
    /// Resolves one icon choice to its cell, reading the theme palette and the localized name <b>live</b> on each call.
    /// Because <see cref="FolderIconCatalog.Choices"/> stamps every choice with the current theme/language revision, a
    /// theme or language switch re-values each unchanged cell, so the icon-keyed repeater's same-key rebind invokes this
    /// again with the live colours and the re-localized name — preserving grid, scroll, and focus identity.
    /// </summary>
    /// <param name="choice">The icon choice to resolve.</param>
    /// <returns>The resolved cell view.</returns>
    private IconCellView BuildCell(IconChoice choice)
    {
        ThemeColors colors = _main.Window.Theme.Colors;
        return new IconCellView(
            choice.Symbol,
            choice.IsSelected
                ? ButtonPalette.FromTheme(colors, ButtonColorScheme.Accent)
                : ButtonPalette.FromTheme(colors, ButtonColorScheme.Standard),
            Brush.CreateSolid(choice.IsSelected ? colors.TextOnAccentFillColorPrimary : colors.TextFillColorPrimary),
            () => SelectedIcon = choice.Icon,
            new AutomationInfo
            {
                Name = FolderIconNames.Resolve(choice.Icon),
                AutomationId = $"RodelTimer.Icon.{choice.Icon}",
                ItemStatus = choice.IsSelected ? Resources.SelectedIconStatus : null,
            });
    }

    private void SetName(string value)
    {
        FolderName = value ?? string.Empty;
        NameChanged?.Invoke(FolderName);
    }
}
