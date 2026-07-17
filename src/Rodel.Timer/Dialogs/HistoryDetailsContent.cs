using System.Globalization;

using Rodel.Timer.Core.History;
using Rodel.Timer.Presentation;
using Rodel.Timer.Styling;

using Sprout;
using Sprout.Controls;
using Sprout.Graphics;
using Sprout.Semantics;
using Sprout.Theming;
using Sprout.Widgets;

using static Sprout.Markup;

namespace Rodel.Timer.Dialogs;

/// <summary>
/// The live content of the session-details dialog — the editable title and note over the read-only folder, recorded
/// time, and duration of one history entry. It owns the edited title and comment as reactive state; the host reads
/// <see cref="Title"/> and <see cref="Comment"/> on commit.
/// </summary>
public sealed partial class HistoryDetailsContent : Control
{
    private readonly MainView _main;
    private readonly string _folderName;
    private readonly string _recordedAt;
    private readonly string _duration;

    /// <summary>Creates the details content for one entry.</summary>
    /// <param name="main">The state-owning root view (the live theme colours and language stamp).</param>
    /// <param name="entry">The history entry to show and edit.</param>
    /// <param name="folderName">The display name of the entry's owning folder.</param>
    /// <exception cref="ArgumentNullException"><paramref name="main"/> or <paramref name="entry"/> is <see langword="null"/>.</exception>
    public HistoryDetailsContent(MainView main, TimerEntry entry, string folderName)
    {
        _main = main ?? throw new ArgumentNullException(nameof(main));
        ArgumentNullException.ThrowIfNull(entry);

        Title = entry.Title;
        Comment = entry.Comment ?? string.Empty;
        _folderName = folderName ?? string.Empty;
        _recordedAt = entry.CreatedAt.LocalDateTime.ToString("f", CultureInfo.CurrentCulture);
        _duration = TimerFormat.Clock(entry.Duration);
    }

    /// <summary>The edited session title (read by the host on commit).</summary>
    public partial string Title { get; private set; }

    /// <summary>Raised (with the new title) whenever the edited title changes, so the host can enable/disable its commit button.</summary>
    public event Action<string>? TitleChanged;

    /// <summary>The edited session note, trimmed by the entry on commit (read by the host on commit).</summary>
    public partial string Comment { get; private set; }

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
            Stack(
                Text(Resources.SessionTitle, LabelStyle, Colors.TextFillColorSecondary),
                TextBox()
                    .Text(Title)
                    .OnTextChanged(SetTitle)
                    .Placeholder(Resources.SessionTitlePlaceholder)
                    .Automation(new AutomationInfo
                    {
                        Name = Resources.SessionTitle,
                        AutomationId = "RodelTimer.HistoryDetails.Title",
                    }))
                .Spacing(TimerVisuals.Spacing.XSmall),
            Border(
                Stack(
                    Stack(
                        Text(Resources.Folders, LabelStyle, Colors.TextFillColorSecondary).Weight(1f),
                        Text(_folderName, ValueStyle, Colors.TextFillColorPrimary))
                        .Orientation(Orientation.Horizontal)
                        .Spacing(TimerVisuals.Spacing.Medium),
                    Stack(
                        Text(Resources.RecordedAt, LabelStyle, Colors.TextFillColorSecondary).Weight(1f),
                        Text(_recordedAt, ValueStyle, Colors.TextFillColorPrimary))
                        .Orientation(Orientation.Horizontal)
                        .Spacing(TimerVisuals.Spacing.Medium),
                    Stack(
                        Text(Resources.Duration, LabelStyle, Colors.TextFillColorSecondary).Weight(1f),
                        Text(_duration, ValueStyle, Colors.TextFillColorPrimary))
                        .Orientation(Orientation.Horizontal)
                        .Spacing(TimerVisuals.Spacing.Medium))
                    .Spacing(TimerVisuals.Spacing.Small))
                .Background(Brush.CreateSolid(Colors.CardBackgroundFillColorDefault))
                .CornerRadius(TimerVisuals.Radius.Control)
                .Padding(14f, 12f, 14f, 12f),
            Stack(
                Text(Resources.Comment, LabelStyle, Colors.TextFillColorSecondary),
                TextBox()
                    .Text(Comment)
                    .OnTextChanged(value => Comment = value ?? string.Empty)
                    .Placeholder(Resources.CommentPlaceholder)
                    .AcceptsReturn()
                    .Height(96f)
                    .Automation(new AutomationInfo
                    {
                        Name = Resources.Comment,
                        AutomationId = "RodelTimer.HistoryDetails.Comment",
                    }))
                .Spacing(TimerVisuals.Spacing.XSmall))
            .Spacing(TimerVisuals.Spacing.Medium)
            .Width(TimerVisuals.MaxWidth.FolderDialog));

    private void SetTitle(string? value)
    {
        Title = value ?? string.Empty;
        TitleChanged?.Invoke(Title);
    }

    internal static TextFormat LabelStyle =>
        new(TextFormat.DefaultFontFamily, 12f, TextFormat.NormalWeight);

    internal static TextFormat ValueStyle =>
        new(TextFormat.DefaultFontFamily, 14f, TextFormat.SemiBoldWeight);
}
