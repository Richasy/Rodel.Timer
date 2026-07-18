using System.Collections.Generic;
using System.Linq;

using Rodel.Timer.Core.Folders;
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
/// The live content of the move-session dialog — a single combo box listing every folder the session can be moved to.
/// It owns the chosen folder index as reactive state; the host reads <see cref="SelectedFolderId"/> on commit.
/// </summary>
public sealed partial class MoveHistoryContent : Control
{
    private readonly MainView _main;
    private readonly IReadOnlyList<TimerFolder> _folders;

    /// <summary>Creates the move-session content over the current folders.</summary>
    /// <param name="main">The state-owning root view (the folders + theme).</param>
    /// <param name="currentFolderId">The folder the session currently lives in (the initial selection).</param>
    /// <exception cref="ArgumentNullException"><paramref name="main"/> is <see langword="null"/>.</exception>
    public MoveHistoryContent(MainView main, Guid currentFolderId)
    {
        _main = main ?? throw new ArgumentNullException(nameof(main));
        _folders = main.Folders;

        int index = 0;
        for (int i = 0; i < _folders.Count; i++)
        {
            if (_folders[i].Id == currentFolderId)
            {
                index = i;
                break;
            }
        }

        SelectedIndex = index;
    }

    /// <summary>
    /// The live theme colours resolved from the owning view's current theme; reading <see cref="MainView.ThemeStamp"/>
    /// subscribes this dialog's body so an in-place theme switch re-renders it with the live palette.
    /// </summary>
    private ThemeColors Colors
    {
        get
        {
            _ = _main.ThemeStamp;
            return _main.Window.Theme.Colors;
        }
    }

    /// <summary>The selected target-folder index into the current folders.</summary>
    public partial int SelectedIndex { get; private set; }

    /// <summary>The selected target folder's id, or <see cref="Guid.Empty"/> when there are no folders.</summary>
    public Guid SelectedFolderId =>
        SelectedIndex >= 0 && SelectedIndex < _folders.Count ? _folders[SelectedIndex].Id : Guid.Empty;

    /// <inheritdoc/>
    public Ui Body => Overlay(
        Stack(
            Text(
                Resources.MoveToFolder,
                new TextFormat(TextFormat.DefaultFontFamily, 13f, TextFormat.SemiBoldWeight),
                Colors.TextFillColorSecondary),
            ComboBox(_folders.Select(f => f.Name))
                .SelectedIndex(SelectedIndex)
                .OnSelectionChanged(OnSelectionChanged)
                .MaxDropDownHeight(320f)
                .Automation(new AutomationInfo
                {
                    Name = Resources.MoveToFolder,
                    AutomationId = TimerAutomationIds.MoveHistoryFolder,
                }))
            .Spacing(TimerVisuals.Spacing.Small)
            .Width(TimerVisuals.MaxWidth.Caption));

    private void OnSelectionChanged(int index) => SelectedIndex = index;
}
