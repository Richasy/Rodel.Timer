using Sprout.Graphics;
using Sprout.Input;
using Sprout.Reconcile;
using Sprout.Styling;
using Sprout.Theming;

namespace Rodel.Timer.Styling;

/// <summary>
/// The app-local visual token table for Rodel Timer — a single source of truth for the spacing rhythm, the corner-radius
/// hierarchy, the standard control heights, the icon sizes, the layout max-widths, and a pair of theme-aware button
/// palettes (transparent command chrome plus selected states). It is deliberately app-local: it does
/// <b>not</b> mutate any Sprout global default, so it styles only this product and leaves the framework's own defaults
/// untouched. Every palette helper resolves from a live <see cref="ThemeColors"/> so a light / dark / high-contrast
/// switch re-colours it through the normal theme-stamp re-render.
/// </summary>
internal static class TimerVisuals
{
    /// <summary>The 4 / 8 / 12 / 16 / 24 / 32 epx spacing rhythm used for gaps, padding, and stack spacing.</summary>
    internal static class Spacing
    {
        /// <summary>4 epx — the tightest gap (a label over its value, an icon's optical nudge).</summary>
        internal const float XSmall = 4f;

        /// <summary>8 epx — the default gap between related controls in a stack.</summary>
        internal const float Small = 8f;

        /// <summary>12 epx — a comfortable gap between row content and its padding.</summary>
        internal const float Medium = 12f;

        /// <summary>16 epx — the section gap and the standard pane / row content padding.</summary>
        internal const float Large = 16f;

        /// <summary>24 epx — hero-card vertical padding and generous section separation.</summary>
        internal const float XLarge = 24f;

        /// <summary>32 epx — hero-card horizontal padding and the widest breathing room.</summary>
        internal const float XXLarge = 32f;
    }

    /// <summary>The corner-radius hierarchy: app 4, control 8, card 12, hero 16, circle 28 (a 56 epx pill).</summary>
    internal static class Radius
    {
        /// <summary>4 epx — the WinUI control-corner radius for inputs and the smallest chrome.</summary>
        internal const float App = 4f;

        /// <summary>8 epx — icon buttons, row tiles, and command chips.</summary>
        internal const float Control = 8f;

        /// <summary>12 epx — cards, the footer command dock, and history rows.</summary>
        internal const float Card = 12f;

        /// <summary>16 epx — the central timer hero card.</summary>
        internal const float Hero = 16f;

        /// <summary>28 epx — a perfect circle for the 56 epx start / stop button (half its diameter).</summary>
        internal const float Circle = 28f;

    }

    /// <summary>The standard control heights: 40 default, 36 icon / compact, 56 hero.</summary>
    internal static class Height
    {
        /// <summary>40 epx — the standard interactive control height (buttons, inputs, dock commands).</summary>
        internal const float Standard = 40f;

        /// <summary>36 epx — the denser height for icon buttons and secondary chips.</summary>
        internal const float Icon = 36f;

        /// <summary>56 epx — the hero start / stop circle diameter and the clock band.</summary>
        internal const float Hero = 56f;

        /// <summary>64 epx — the open-stage primary start / stop action diameter (the prototype's roomier hero button).</summary>
        internal const float PrimaryAction = 64f;

    }

    /// <summary>The icon glyph sizes: 16 small, 20 medium, 24 large, plus the 40 epx square row tile.</summary>
    internal static class IconSize
    {
        /// <summary>16 epx — inline glyphs beside a label.</summary>
        internal const float Small = 16f;

        /// <summary>20 epx — row and header glyphs.</summary>
        internal const float Medium = 20f;

        /// <summary>24 epx — the primary action glyph (start / stop).</summary>
        internal const float Large = 24f;

        /// <summary>36 epx — the square glyph tile that leads a folder / history row.</summary>
        internal const float Tile = 36f;
    }

    /// <summary>The rolling-clock font sizes: 56 in the hero, 40 in the compact overlay.</summary>
    internal static class Clock
    {
        /// <summary>56 epx — the hero clock digits.</summary>
        internal const float Hero = 56f;

        /// <summary>40 epx — the compact-overlay clock digits.</summary>
        internal const float Compact = 40f;
    }

    /// <summary>The layout max-widths that keep content from stretching edge-to-edge on a wide window.</summary>
    internal static class MaxWidth
    {
        /// <summary>560 epx — the central timer hero card.</summary>
        internal const float Hero = 560f;

        /// <summary>420 epx — the session-title input and the hero's inner content column.</summary>
        internal const float HeroColumn = 420f;

        /// <summary>500 epx — the open-stage session-title field's maximum width (the prototype's <c>min(500, 100%)</c>).</summary>
        internal const float SessionField = 500f;

        /// <summary>500 epx — the settings dialog content column.</summary>
        internal const float Dialog = 500f;

        /// <summary>440 epx — the add / edit-folder dialog content column.</summary>
        internal const float FolderDialog = 440f;

        /// <summary>360 epx — a wrapping caption (empty-state, message detail).</summary>
        internal const float Caption = 360f;

    }

    /// <summary>The row geometry: fixed row extents and the selected-folder accent indicator size.</summary>
    internal static class Row
    {
        /// <summary>50 epx — a folder row's extent (36 tile + 2×7 padding, matching the prototype's <c>.folder-row</c>).</summary>
        internal const float Folder = 50f;

        /// <summary>50 epx — a history card's extent (36 tile + 2×7 padding, matching the prototype's <c>.history-row</c>).</summary>
        internal const float History = 50f;

        /// <summary>The selected-folder left accent indicator width (3 epx).</summary>
        internal const float IndicatorWidth = 3f;

        /// <summary>The selected-folder left accent indicator height (24 epx).</summary>
        internal const float IndicatorHeight = 24f;
    }

    /// <summary>
    /// A transparent "ghost" button palette — no resting fill or border, a subtle fill on hover / press, matching WinUI's
    /// subtle command style. Used for the header settings icon, the footer dock commands, the compact-exit chip, and every
    /// row "more" / "add" icon button, so the chrome reads as flat until the pointer is over it.
    /// </summary>
    /// <param name="colors">The live theme palette to resolve from.</param>
    /// <returns>The four-state ghost palette.</returns>
    internal static ButtonPalette Ghost(ThemeColors colors) => new(
        new ButtonVisualState(colors.SubtleFillColorTransparent, colors.TextFillColorPrimary, colors.SubtleFillColorTransparent),
        new ButtonVisualState(colors.SubtleFillColorSecondary, colors.TextFillColorPrimary, colors.SubtleFillColorTransparent),
        new ButtonVisualState(colors.SubtleFillColorTertiary, colors.TextFillColorSecondary, colors.SubtleFillColorTransparent),
        new ButtonVisualState(colors.SubtleFillColorDisabled, colors.TextFillColorDisabled, colors.SubtleFillColorTransparent));

    /// <summary>
    /// A title-bar toggle palette with the same transparent unchecked chrome as <see cref="Ghost"/> and the standard
    /// accent checked state, so the pin command is flat at rest while its active state remains unambiguous.
    /// </summary>
    internal static ToggleButtonPalette GhostToggle(ThemeColors colors)
    {
        var uncheckedPalette = new ToggleButtonStatePalette(
            new ToggleButtonVisualState(colors.SubtleFillColorTransparent, colors.TextFillColorPrimary, colors.SubtleFillColorTransparent),
            new ToggleButtonVisualState(colors.SubtleFillColorSecondary, colors.TextFillColorPrimary, colors.SubtleFillColorTransparent),
            new ToggleButtonVisualState(colors.SubtleFillColorTertiary, colors.TextFillColorSecondary, colors.SubtleFillColorTransparent),
            new ToggleButtonVisualState(colors.SubtleFillColorDisabled, colors.TextFillColorDisabled, colors.SubtleFillColorTransparent));
        ToggleButtonPalette palette = ToggleButtonPalette.FromTheme(colors);
        return palette with { Unchecked = uncheckedPalette, Indeterminate = uncheckedPalette };
    }

    /// <summary>
    /// The named, immutable <see cref="ButtonStyle"/> values this product applies to its buttons through the canonical
    /// <c>.Style(...)</c> verb — a single app style vocabulary rather than a bespoke chrome extension. Each is a sparse
    /// overlay: it sets only the corner radius and — critically — <b>zero padding</b>, so a fixed-square icon button
    /// (e.g. 36×36) centres its glyph instead of inheriting the Sprout Button default inset (11,5,11,6). That default
    /// reserves 22 epx horizontally, which crops a 20 epx glyph inside a 36 epx button; zeroing it lets the fixed square
    /// centre the glyph with a symmetric margin. Apply the style in the <see cref="ButtonView"/> region of the fluent
    /// chain — <b>before</b> <c>.Width</c>/<c>.Height</c>, which erase the type to <c>Ui</c> (a later modifier would then
    /// bind to a layout wrapper the reconciler generator cannot see through). Colours are left to the framework's own
    /// theme-aware default; a product-state fill is supplied separately through <see cref="Ghost"/>.
    /// </summary>
    internal static class Buttons
    {
        /// <summary>A fixed-square icon button — zero padding + the 8 epx control radius. Centres a glyph in a 36 / 40 / 44 epx square.</summary>
        internal static readonly ButtonStyle Icon = default(ButtonStyle).Padding(0f).CornerRadius(Radius.Control);

        /// <summary>The circular hero start / stop action — zero padding + the 28 epx radius (a perfect 56 epx pill).</summary>
        internal static readonly ButtonStyle CircularAction = default(ButtonStyle).Padding(0f).CornerRadius(Radius.Circle);

    }

    /// <summary>Named toggle-button styles used by the product chrome.</summary>
    internal static class ToggleButtons
    {
        /// <summary>A fixed-square icon toggle with zero padding and the standard control radius.</summary>
        internal static readonly ToggleButtonStyle Icon = default(ToggleButtonStyle).Padding(0f).CornerRadius(Radius.Control);
    }
}
