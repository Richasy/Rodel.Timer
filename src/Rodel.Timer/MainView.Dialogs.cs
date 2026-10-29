using Rodel.Timer.Core.Bootstrap;
using Rodel.Timer.Core.Folders;
using Rodel.Timer.Core.History;
using Rodel.Timer.Core.Persistence;
using Rodel.Timer.Core.Settings;
using Rodel.Timer.Dialogs;
using Rodel.Timer.Presentation;

using Sprout.Controls;
using Sprout.Graphics;
using Sprout.Popups;
using Sprout.Semantics;
using Sprout.Widgets;

using static Sprout.Markup;

namespace Rodel.Timer;

/// <summary>Dialog and menu surface of <see cref="MainView"/> (folder editing, history commands, settings, what's new).</summary>
public sealed partial class MainView
{
    /// <summary>
    /// The current product version, compared against the persisted last-seen version for the what's-new dialog. Bump it
    /// in the same change as the <c>SproutAppxVersion</c> / <c>Package.appxmanifest</c> version whenever a release adds
    /// something the notice should announce — the notice fires precisely on the mismatch between this and the value
    /// persisted by the previous run, so an unbumped version means an upgrade ships silently.
    /// </summary>
    internal const string CurrentVersion = "2.2609.2.0";

    /// <summary>
    /// The release the shipped <see cref="Resources.WhatsNewBody"/> describes. It is deliberately <b>separate</b> from
    /// <see cref="CurrentVersion"/>: bump this one only when the notice text itself is rewritten, so a later
    /// bug-fix release raises <see cref="CurrentVersion"/> and upgrades silently instead of replaying these notes at a
    /// user who has already read them.
    /// </summary>
    internal const string WhatsNewVersion = "2.2609.2.0";

    /// <summary>
    /// The version shown on the About surface. When the app is packaged this is the installed <b>package identity</b>
    /// version (the full four-part <c>Major.Minor.Build.Revision</c>, straight from <c>Package.appxmanifest</c> via
    /// <c>Package.Current.Id.Version</c>), so it can never drift from the shipped package; an unpackaged developer build
    /// has no package identity and falls back to <see cref="CurrentVersion"/> normalized to four parts. Resolved once —
    /// the package version does not change while the app runs.
    /// </summary>
    internal static string DisplayVersion { get; } = ResolveDisplayVersion();

    private static string ResolveDisplayVersion()
    {
        if (Sprout.PackageIdentity.IsPackaged)
        {
            Windows.ApplicationModel.PackageVersion version = Windows.ApplicationModel.Package.Current.Id.Version;
            return $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
        }

        // An unpackaged developer build has no package identity — normalize the product const to four parts so the
        // displayed shape matches the packaged app (which reads the four-part manifest version).
        System.Version parsed = System.Version.Parse(CurrentVersion);
        return $"{parsed.Major}.{parsed.Minor}.{(parsed.Build < 0 ? 0 : parsed.Build)}.{(parsed.Revision < 0 ? 0 : parsed.Revision)}";
    }

    /// <summary>Builds a fresh per-folder context menu from the latest store state.</summary>
    /// <param name="folderId">The folder the menu acts on.</param>
    /// <returns>The owned child menu for the row context gesture.</returns>
    public MenuFlyout BuildFolderContextMenu(Guid folderId)
    {
        TimerFolder folder = _store.Folders.FirstOrDefault(candidate => candidate.Id == folderId)
            ?? throw new KeyNotFoundException($"Folder '{folderId}' does not exist.");
        var edit = new MenuFlyoutItem(Resources.EditFolder, () => ShowEditFolderDialog(folderId))
        {
            Automation = new AutomationInfo { AutomationId = TimerAutomationIds.FolderEdit(folderId) },
        };
        var delete = new MenuFlyoutItem(Resources.DeleteFolder, () => ConfirmDeleteFolder(folderId))
        {
            IsEnabled = _store.Folders.Count > 1,
            Automation = new AutomationInfo { AutomationId = TimerAutomationIds.FolderDelete(folderId) },
        };
        return new MenuFlyout(edit, delete)
        {
            Placement = PopupPlacement.RightEdgeAlignedTop,
        }
            .Automation(new AutomationInfo
            {
                Name = FormatOne(Resources.MoreActions, folder.Name),
                AutomationId = TimerAutomationIds.FolderMenu(folderId),
            });
    }

    /// <summary>Builds a fresh per-session context menu from the latest store state.</summary>
    /// <param name="entryId">The history entry the menu acts on.</param>
    /// <returns>The owned child menu for the row context gesture.</returns>
    public MenuFlyout BuildHistoryContextMenu(Guid entryId)
    {
        TimerEntry entry = _store.Entries.FirstOrDefault(candidate => candidate.Id == entryId)
            ?? throw new KeyNotFoundException($"History entry '{entryId}' does not exist.");
        var edit = new MenuFlyoutItem(Resources.EditHistory, () => ShowHistoryDetailsDialog(entryId))
        {
            Automation = new AutomationInfo { AutomationId = TimerAutomationIds.HistoryEdit(entryId) },
        };
        var move = new MenuFlyoutItem(Resources.MoveHistory, () => ShowMoveHistoryDialog(entryId))
        {
            IsEnabled = _store.Folders.Count > 1,
            Automation = new AutomationInfo { AutomationId = TimerAutomationIds.HistoryMove(entryId) },
        };
        var delete = new MenuFlyoutItem(Resources.DeleteHistory, () => ConfirmDeleteHistory(entryId))
        {
            Automation = new AutomationInfo { AutomationId = TimerAutomationIds.HistoryDelete(entryId) },
        };
        return new MenuFlyout(edit, move, delete)
        {
            Placement = PopupPlacement.RightEdgeAlignedTop,
        }
            .Automation(new AutomationInfo
            {
                Name = FormatOne(Resources.MoreActions, entry.Title),
                AutomationId = TimerAutomationIds.HistoryMenu(entryId),
            });
    }

    /// <summary>Opens the add-folder dialog and adds the folder on commit.</summary>
    public async void ShowAddFolderDialog()
    {
        if (!_store.Bootstrap.IsSuccess)
        {
            return;
        }

        try
        {
            var content = new FolderEditContent(this, string.Empty, TimerFolderIcon.Folder);
            var dialog = new ContentDialog
            {
                Title = Resources.AddFolder,
                ContentComponent = () => content,
                PrimaryButtonText = Resources.Save,
                CloseButtonText = Resources.Cancel,
                DefaultButton = ContentDialogButton.Primary,
                IsPrimaryButtonEnabled = false,
            };
            content.NameChanged += name => dialog.IsPrimaryButtonEnabled = !string.IsNullOrWhiteSpace(name);
            dialog.PrimaryButtonClick += args =>
            {
                if (string.IsNullOrWhiteSpace(content.FolderName))
                {
                    args.Cancel = true;
                }
            };

            if (await dialog.ShowAsync(_window).ConfigureAwait(true) == ContentDialogResult.Primary)
            {
                _store.AddFolder(content.FolderName.Trim(), content.SelectedIcon);
                Announce(Resources.FolderAddedAnnouncement);
            }
        }
        catch (Exception exception) when (IsDataException(exception))
        {
            ShowSaveError(exception.Message);
        }
    }

    /// <summary>Opens the edit-folder dialog for a folder and applies the change on commit.</summary>
    /// <param name="folderId">The folder to edit.</param>
    public async void ShowEditFolderDialog(Guid folderId)
    {
        if (!_store.Bootstrap.IsSuccess || !TryGetFolder(folderId, out TimerFolder folder))
        {
            return;
        }

        try
        {
            var content = new FolderEditContent(this, folder.Name, folder.Icon);
            var dialog = new ContentDialog
            {
                Title = Resources.EditFolder,
                ContentComponent = () => content,
                PrimaryButtonText = Resources.Save,
                CloseButtonText = Resources.Cancel,
                DefaultButton = ContentDialogButton.Primary,
                IsPrimaryButtonEnabled = !string.IsNullOrWhiteSpace(folder.Name),
            };
            content.NameChanged += name => dialog.IsPrimaryButtonEnabled = !string.IsNullOrWhiteSpace(name);
            dialog.PrimaryButtonClick += args =>
            {
                if (string.IsNullOrWhiteSpace(content.FolderName))
                {
                    args.Cancel = true;
                }
            };

            if (await dialog.ShowAsync(_window).ConfigureAwait(true) == ContentDialogResult.Primary)
            {
                _store.UpdateFolder(folderId, content.FolderName.Trim(), content.SelectedIcon);
                Announce(Resources.FolderUpdatedAnnouncement);
            }
        }
        catch (Exception exception) when (IsDataException(exception))
        {
            ShowSaveError(exception.Message);
        }
    }

    /// <summary>Confirms and deletes a folder (rejecting deletion of the last remaining folder).</summary>
    /// <param name="folderId">The folder to delete.</param>
    public async void ConfirmDeleteFolder(Guid folderId)
    {
        if (!_store.Bootstrap.IsSuccess)
        {
            return;
        }

        if (_store.Folders.Count <= 1)
        {
            var notice = new ContentDialog
            {
                Title = Resources.DeleteFolder,
                Content = () => Text(Resources.LastFolderError, BodyStyle),
                CloseButtonText = Resources.Done,
                DefaultButton = ContentDialogButton.Close,
            };
            await notice.ShowAsync(_window).ConfigureAwait(true);
            return;
        }

        try
        {
            var dialog = new ContentDialog
            {
                Title = Resources.DeleteFolderTitle,
                Content = () => Text(Resources.DeleteFolderMessage, BodyStyle),
                PrimaryButtonText = Resources.Delete,
                CloseButtonText = Resources.Cancel,
                DefaultButton = ContentDialogButton.Close,
            };

            if (await dialog.ShowAsync(_window).ConfigureAwait(true) == ContentDialogResult.Primary)
            {
                _store.RemoveFolder(folderId);
                Announce(Resources.FolderDeletedAnnouncement);
            }
        }
        catch (Exception exception) when (IsDataException(exception) || exception is InvalidOperationException)
        {
            ShowSaveError(exception.Message);
        }
    }

    /// <summary>Opens the move-session dialog and moves the entry to the chosen folder on commit.</summary>
    /// <param name="entryId">The history entry to move.</param>
    public async void ShowMoveHistoryDialog(Guid entryId)
    {
        if (!_store.Bootstrap.IsSuccess || !TryGetEntry(entryId, out TimerEntry entry))
        {
            return;
        }

        try
        {
            var content = new MoveHistoryContent(this, entry.FolderId);
            var dialog = new ContentDialog
            {
                Title = Resources.MoveHistory,
                ContentComponent = () => content,
                PrimaryButtonText = Resources.Done,
                CloseButtonText = Resources.Cancel,
                DefaultButton = ContentDialogButton.Primary,
            }.Automation(new AutomationInfo { AutomationId = TimerAutomationIds.MoveHistoryDialog });

            if (await dialog.ShowAsync(_window).ConfigureAwait(true) == ContentDialogResult.Primary
                && content.SelectedFolderId != Guid.Empty
                && content.SelectedFolderId != entry.FolderId)
            {
                _store.MoveEntry(entryId, content.SelectedFolderId);
                Announce(Resources.HistoryMovedAnnouncement);
            }
        }
        catch (Exception exception) when (IsDataException(exception))
        {
            ShowSaveError(exception.Message);
        }
    }

    /// <summary>Opens the session-details dialog and applies the edited title/comment on commit.</summary>
    /// <param name="entryId">The history entry to show and edit.</param>
    public async void ShowHistoryDetailsDialog(Guid entryId)
    {
        if (!_store.Bootstrap.IsSuccess || !TryGetEntry(entryId, out TimerEntry entry))
        {
            return;
        }

        try
        {
            string folderName = TryGetFolder(entry.FolderId, out TimerFolder folder) ? folder.Name : string.Empty;
            var content = new HistoryDetailsContent(this, entry, folderName);
            var dialog = new ContentDialog
            {
                Title = Resources.HistoryDetails,
                ContentComponent = () => content,
                PrimaryButtonText = Resources.Save,
                CloseButtonText = Resources.Cancel,
                DefaultButton = ContentDialogButton.Primary,
                IsPrimaryButtonEnabled = !string.IsNullOrWhiteSpace(entry.Title),
            }.Automation(new AutomationInfo { AutomationId = "RodelTimer.HistoryDetails.Dialog" });
            content.TitleChanged += title => dialog.IsPrimaryButtonEnabled = !string.IsNullOrWhiteSpace(title);
            dialog.PrimaryButtonClick += args =>
            {
                if (string.IsNullOrWhiteSpace(content.Title))
                {
                    args.Cancel = true;
                }
            };

            if (await dialog.ShowAsync(_window).ConfigureAwait(true) == ContentDialogResult.Primary)
            {
                _store.UpdateEntry(entryId, content.Title.Trim(), content.Comment);
                Announce(Resources.HistoryUpdatedAnnouncement);
            }
        }
        catch (Exception exception) when (IsDataException(exception))
        {
            ShowSaveError(exception.Message);
        }
    }

    /// <summary>Confirms and deletes a history entry.</summary>
    /// <param name="entryId">The history entry to delete.</param>
    public async void ConfirmDeleteHistory(Guid entryId)
    {
        if (!_store.Bootstrap.IsSuccess)
        {
            return;
        }

        try
        {
            var dialog = new ContentDialog
            {
                Title = Resources.DeleteHistoryTitle,
                Content = () => Text(Resources.DeleteHistoryMessage, BodyStyle),
                PrimaryButtonText = Resources.Delete,
                CloseButtonText = Resources.Cancel,
                DefaultButton = ContentDialogButton.Close,
            };

            if (await dialog.ShowAsync(_window).ConfigureAwait(true) == ContentDialogResult.Primary)
            {
                _store.RemoveEntry(entryId);
                Announce(Resources.HistoryDeletedAnnouncement);
            }
        }
        catch (Exception exception) when (IsDataException(exception))
        {
            ShowSaveError(exception.Message);
        }
    }

    /// <summary>Opens the settings dialog (theme, language, font, and daily target apply immediately).</summary>
    public async void ShowSettings()
    {
        if (!_store.Bootstrap.IsSuccess)
        {
            return;
        }

        try
        {
            ContentDialog dialog = null!;

            // On an in-dialog language switch, relabel the dialog's live-settable chrome (title + close button) so the
            // whole surface follows the new language without a hide/reopen. The dialog Pane keeps a stable AutomationId
            // (below) and no explicit Name, so it is named by the live-updating Title.
            var content = new SettingsContent(this, () =>
            {
                dialog.Title = Resources.Settings;
                dialog.CloseButtonText = Resources.Done;
            });
            dialog = new ContentDialog
            {
                Title = Resources.Settings,
                ContentComponent = () => content,
                CloseButtonText = Resources.Done,
                DefaultButton = ContentDialogButton.Close,
            }.Automation(new AutomationInfo { AutomationId = TimerAutomationIds.SettingsDialog });
            await dialog.ShowAsync(_window).ConfigureAwait(true);
        }
        catch (Exception exception) when (IsDataException(exception))
        {
            ShowSaveError(exception.Message);
        }
    }

    /// <summary>
    /// Shows the one-shot startup notices in order after the workspace loads: first any migration warnings (when the
    /// workspace was migrated and repair raised warnings), then the what's-new notice per <see cref="WhatsNewPolicy"/>.
    /// Both are non-blocking and shown at most once; a fresh install records the version without showing what's new.
    /// </summary>
    public async void ShowStartupNotices()
    {
        if (!_store.Bootstrap.IsSuccess)
        {
            return;
        }

        try
        {
            TimerBootstrapResult bootstrap = _store.Bootstrap.Value!;
            if (MigrationNotice.ShouldShow(bootstrap))
            {
                await ShowMigrationWarningsAsync(bootstrap.Warnings).ConfigureAwait(true);
            }

            await ApplyWhatsNewAsync().ConfigureAwait(true);
        }
        catch (Exception exception) when (IsDataException(exception))
        {
            ShowSaveError(exception.Message);
        }
    }

    /// <summary>Shows the migration-warning notice (header count + each warning line) without blocking readiness.</summary>
    /// <param name="warnings">The non-fatal migration warnings to surface.</param>
    private async Task ShowMigrationWarningsAsync(IReadOnlyList<string> warnings)
    {
        var body = string.Join(
            Environment.NewLine,
            new[] { FormatOne(Resources.MigrationWarnings, warnings.Count.ToString(System.Globalization.CultureInfo.CurrentCulture)) }
                .Concat(warnings.Select(static warning => "\u2022 " + warning)));

        var dialog = new ContentDialog
        {
            Title = Resources.MigrationWarningsTitle,
            Content = () => Text(body, BodyStyle),
            CloseButtonText = Resources.Done,
            DefaultButton = ContentDialogButton.Close,
        };
        await dialog.ShowAsync(_window).ConfigureAwait(true);
    }

    /// <summary>
    /// Applies the what's-new policy: shows the dialog and records the version for an upgrade / migration, records the
    /// version silently for a fresh install, or does nothing when the version was already seen.
    /// </summary>
    private async Task ApplyWhatsNewAsync()
    {
        WhatsNewAction action = WhatsNewPolicy.Decide(
            _store.Bootstrap.Value!.Origin,
            _store.Preferences.LastSeenVersion,
            CurrentVersion,
            WhatsNewVersion);

        switch (action)
        {
            case WhatsNewAction.Show:
                var dialog = new ContentDialog
                {
                    Title = Resources.WhatsNewTitle,
                    Content = () => Text(Resources.WhatsNewBody, BodyStyle),
                    CloseButtonText = Resources.Done,
                    DefaultButton = ContentDialogButton.Close,
                };
                await dialog.ShowAsync(_window).ConfigureAwait(true);
                _store.UpdateLastSeenVersion(CurrentVersion);
                break;

            case WhatsNewAction.RecordOnly:
                _store.UpdateLastSeenVersion(CurrentVersion);
                break;

            case WhatsNewAction.None:
            default:
                break;
        }
    }

    private static bool IsDataException(Exception exception) =>
        exception is TimerDataException or IOException or UnauthorizedAccessException;

    private static TextFormat BodyStyle =>
        new(TextFormat.DefaultFontFamily, 14f, TextFormat.NormalWeight, TextAlignment.Leading, TextWrapping.Wrap);

    private bool TryGetFolder(Guid folderId, out TimerFolder folder)
    {
        foreach (TimerFolder candidate in _store.Folders)
        {
            if (candidate.Id == folderId)
            {
                folder = candidate;
                return true;
            }
        }

        folder = null!;
        return false;
    }

    private bool TryGetEntry(Guid entryId, out TimerEntry entry)
    {
        foreach (TimerEntry candidate in _store.SelectedEntries)
        {
            if (candidate.Id == entryId)
            {
                entry = candidate;
                return true;
            }
        }

        entry = null!;
        return false;
    }
}
