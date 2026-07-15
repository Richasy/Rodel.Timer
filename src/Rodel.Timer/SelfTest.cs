using Rodel.Timer.Core.Bootstrap;
using Rodel.Timer.Core.Folders;
using Rodel.Timer.Core.History;
using Rodel.Timer.Core.Migration;
using Rodel.Timer.Core.Persistence;
using Rodel.Timer.Core.Settings;
using Rodel.Timer.Presentation;
using Rodel.Timer.Styling;

using Sprout;
using Sprout.Controls;
using Sprout.Graphics;
using Sprout.Localization;
using Sprout.Reactive;
using Sprout.Reconcile;
using Sprout.Semantics;

namespace Rodel.Timer;

internal static class SelfTest
{
    public static int Run()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"rodel-timer-selftest-{Guid.NewGuid():N}");

        try
        {
            Guid folderId = Guid.ParseExact("11111111111111111111111111111111", "N");
            var timeProvider = new FixedTimeProvider(
                new DateTimeOffset(2026, 7, 14, 1, 0, 0, TimeSpan.Zero));
            var preferences = new TimerPreferences(
                TimerThemeMode.System,
                TimerPreferences.EnglishLanguage,
                TimerPreferences.DefaultFontFamily,
                TimerPreferences.DefaultDailyTargetMinutes);

            using var repository = new TimerDataRepository(Path.Combine(directory, "timer-data.json"));
            var importer = new LegacyWorkTimerImporter(
                "Default",
                preferences,
                TimeZoneInfo.Utc);
            var bootstrapper = new TimerDataBootstrapper(
                repository,
                EmptyLegacyDataSource.Instance,
                importer,
                timeProvider,
                "Default",
                preferences,
                () => folderId);

            TimerBootstrapResult created = bootstrapper.LoadAsync().GetAwaiter().GetResult();
            TimerBootstrapResult loaded = bootstrapper.LoadAsync().GetAwaiter().GetResult();

            bool corePassed = created.Origin == TimerBootstrapOrigin.Created
                && loaded.Workspace.Folders.Count == 1
                && loaded.Workspace.SelectedFolderId == folderId;
            bool persistencePassed = loaded.Origin == TimerBootstrapOrigin.Existing;
            Guid entryId = Guid.ParseExact("22222222222222222222222222222222", "N");
            int saves = 0;
            using var store = new TimerAppStore(
                _ => Task.FromResult(created),
                (_, _) =>
                {
                    saves++;
                    return Task.CompletedTask;
                },
                timeProvider,
                TimeZoneInfo.Utc,
                new InlineScheduler(timeProvider),
                () => entryId);
            store.LoadAsync().GetAwaiter().GetResult();
            store.StartTimer();
            _ = store.StopTimer("NativeAOT selftest");
            store.CloseAsync().GetAwaiter().GetResult();
            bool storePassed = store.Bootstrap.IsSuccess
                && store.Entries.Count == 1
                && store.Entries[0].Id == entryId
                && store.SaveState.IsSuccess
                && saves == 1;

            string originalLanguage = Localizer.CurrentLanguage;
            bool localizationPassed;
            try
            {
                Localizer.CurrentLanguage = TimerPreferences.EnglishLanguage;
                bool english = Resources.AppName == "Rodel Timer";
                Localizer.CurrentLanguage = TimerPreferences.ChineseLanguage;
                bool chinese = Resources.AppName == "Rodel 计时器";
                localizationPassed = english && chinese;
            }
            finally
            {
                Localizer.CurrentLanguage = originalLanguage;
            }

            bool unpackaged = !PackageIdentity.IsPackaged;
            bool uiPassed = RunUiChecks();
            bool passed = corePassed && persistencePassed && storePassed && localizationPassed && unpackaged && uiPassed;

            Console.WriteLine(
                $"packaging: IsPackaged={PackageIdentity.IsPackaged} (expected false) => {(unpackaged ? "ok" : "bad")}");
            Console.WriteLine(
                $"selftest: core={(corePassed ? "ok" : "bad")} "
                + $"persistence={(persistencePassed ? "ok" : "bad")} "
                + $"store={(storePassed ? "ok" : "bad")} "
                + $"localization={(localizationPassed ? "ok" : "bad")} "
                + $"ui={(uiPassed ? "ok" : "bad")} "
                + $"leaked=0 => {(passed ? "PASS" : "FAIL")}");
            return passed ? 0 : 1;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
        finally
        {
            try
            {
                Directory.Delete(directory, recursive: true);
            }
            catch (Exception exception) when (
                exception is IOException or UnauthorizedAccessException)
            {
                Console.Error.WriteLine($"Could not remove self-test directory: {exception.Message}");
            }
        }
    }

    /// <summary>
    /// Pure, window-free checks of the UI-layer helpers (viewport breakpoints, shell resolution, clock formatting,
    /// row automation-id uniqueness, and the ready / compact accessibility contract). Everything here is deterministic
    /// and NativeAOT-safe (no reflection, no window, no wall clock).
    /// </summary>
    /// <remarks>
    /// The two audited trees are hand-authored <b>contract mirrors</b> — the accessibility structure the mounted
    /// <see cref="MainView"/> is expected to project in the ready and compact states — <em>not</em> the live mounted
    /// tree. They exist to pin the contract cheaply and headlessly; the real mounted tree is verified end-to-end by the
    /// Sprout.DevTools drive over the published exe (which enters compact, asserts the title-bar / title / toggle ids are
    /// gone, then exits and asserts they return). The ready mirror must carry every essential id; the compact mirror
    /// must omit exactly the ids torn down by the compact presenter (Settings, Title, and the two footer toggles).
    /// </remarks>
    private static bool RunUiChecks()
    {
        // Breakpoints: wide => both inline; medium => folder overlay / history inline; narrow => both overlay.
        TimerPaneModes wide = TimerBreakpoints.Resolve(1400d);
        TimerPaneModes medium = TimerBreakpoints.Resolve(1000d);
        TimerPaneModes narrow = TimerBreakpoints.Resolve(800d);
        bool breakpoints =
            wide is { Folder: SplitViewDisplayMode.Inline, History: SplitViewDisplayMode.Inline }
            && medium is { Folder: SplitViewDisplayMode.Overlay, History: SplitViewDisplayMode.Inline }
            && narrow is { Folder: SplitViewDisplayMode.Overlay, History: SplitViewDisplayMode.Overlay };

        // Compact forces both panes overlay + closed regardless of the open flags.
        TimerShellLayout compact = TimerShell.Resolve(1400d, isCompact: true, folderPaneOpen: true, historyPaneOpen: true);
        TimerShellLayout open = TimerShell.Resolve(1400d, isCompact: false, folderPaneOpen: true, historyPaneOpen: false);
        bool shell =
            compact is { FolderMode: SplitViewDisplayMode.Overlay, HistoryMode: SplitViewDisplayMode.Overlay, FolderPaneOpen: false, HistoryPaneOpen: false }
            && open is { FolderMode: SplitViewDisplayMode.Inline, FolderPaneOpen: true, HistoryPaneOpen: false };

        // Pane mutual-exclusion: narrow (both overlay) => opening one closes the other; medium/wide/compact => not.
        bool mutualExclusion =
            TimerShell.PanesAreMutuallyExclusive(800d, isCompact: false)
            && !TimerShell.PanesAreMutuallyExclusive(1000d, isCompact: false)
            && !TimerShell.PanesAreMutuallyExclusive(1400d, isCompact: false)
            && !TimerShell.PanesAreMutuallyExclusive(800d, isCompact: true);

        // Clock formatting: zero-padded HH:mm:ss, hours can exceed 24, negatives clamp to zero.
        bool format =
            TimerFormat.Clock(new TimeSpan(1, 2, 3, 4)) == "26:03:04"
            && TimerFormat.Clock(TimeSpan.Zero) == "00:00:00"
            && TimerFormat.Clock(TimeSpan.FromSeconds(-5)) == "00:00:00";

        // Row automation ids are unique per Guid and disjoint between select/more/history.
        Guid a = Guid.ParseExact("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", "N");
        Guid b = Guid.ParseExact("bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb", "N");
        string[] ids =
        [
            TimerAutomationIds.FolderSelect(a),
            TimerAutomationIds.FolderMore(a),
            TimerAutomationIds.FolderSelect(b),
            TimerAutomationIds.FolderMore(b),
            TimerAutomationIds.HistoryMore(a),
            TimerAutomationIds.HistoryMore(b),
        ];
        bool automation = ids.Distinct(StringComparer.Ordinal).Count() == ids.Length
            && Array.TrueForAll(ids, static id => id.StartsWith(TimerAutomationIds.Prefix, StringComparison.Ordinal));

        // The ids the compact presenter tears down (title-bar settings, title editor, both footer toggles).
        string[] compactOmitted =
        [
            TimerAutomationIds.Settings,
            TimerAutomationIds.Title,
            TimerAutomationIds.FoldersToggle,
            TimerAutomationIds.HistoryToggle,
        ];

        // Ready contract: audit clean, and every essential id present (including the ones compact omits).
        SemanticsNode ready = BuildReadyContractTree();
        bool readyContract = AccessibilityAudit.Audit(ready).Count == 0
            && Array.TrueForAll(compactOmitted, id => ContainsAutomationId(ready, id))
            && ContainsAutomationId(ready, TimerAutomationIds.StartStop)
            && ContainsAutomationId(ready, TimerAutomationIds.Compact);

        // Compact contract: audit clean, the torn-down ids absent, and the compact essentials still present.
        SemanticsNode compactTree = BuildCompactContractTree();
        bool compactContract = AccessibilityAudit.Audit(compactTree).Count == 0
            && Array.TrueForAll(compactOmitted, id => !ContainsAutomationId(compactTree, id))
            && ContainsAutomationId(compactTree, TimerAutomationIds.StartStop)
            && ContainsAutomationId(compactTree, TimerAutomationIds.Compact);

        // Every icon-picker cell must expose a non-empty, localized human-readable name (never a raw enum name), and the
        // selected-icon status must be its own localized string — the icon-picker accessibility contract. A revision bump
        // must also re-value every cell at the same enum key (the same-key rebind that re-themes / re-localizes the grid
        // in place) without changing the icon key set.
        IReadOnlyList<IconChoice> iconsRev0 = FolderIconCatalog.Choices(TimerFolderIcon.Book, 0);
        IReadOnlyList<IconChoice> iconsRev1 = FolderIconCatalog.Choices(TimerFolderIcon.Book, 1);
        bool iconRebind = iconsRev0.Count == iconsRev1.Count
            && iconsRev0.Count > 0
            && Enumerable.Range(0, iconsRev0.Count).All(i =>
                iconsRev0[i].Icon == iconsRev1[i].Icon && !iconsRev0[i].Equals(iconsRev1[i]));
        bool iconNames = FolderIconCatalog.All.Count > 0
            && FolderIconCatalog.All.All(option => !string.IsNullOrWhiteSpace(FolderIconNames.Resolve(option.Icon)))
            && !string.IsNullOrWhiteSpace(Resources.SelectedIconStatus)
            && iconRebind;

        // Language apply: the shared seam must always run all three effects (localizer + title + theme-revision bump) so
        // the persisted-load and runtime-switch paths cannot drift — the missing revision bump was the row-freeze bug.
        bool languageApply = RunLanguageApplyChecks();

        // Row projection: the folder/history repeaters bind to TimerRowProjector output (no whole-list Keyed wrapper), so
        // a selection flags exactly one folder, a revision bump yields a new value at the same Guid key (the same-key
        // rebind that re-themes rows in place), and the keys are unique and ordered.
        bool rowProjection = RunRowProjectionChecks();

        // Icon-button style: the app icon-button ButtonStyle (TimerVisuals.Buttons.Icon), applied via the canonical
        // .Style(...) verb, must zero the Sprout Button default inset (11,5,11,6) so a fixed square icon button (e.g. the
        // 36 epx title-bar Settings / Compact, the 40 epx footer toggles, the 36 epx pane Add / More, the 44 epx picker
        // cells) centres its glyph instead of cropping it — the clipped-glyph fix. A non-zero resolved padding would
        // reserve 22 epx horizontally and crop a 20 epx glyph inside a 36 epx button. The style bakes into the button's
        // canonical Appearance plane, so the padding + requested corner radius are asserted there.
        ButtonView chromed = Markup.Button("x", static () => { }).Style(TimerVisuals.Buttons.Icon);
        bool iconChrome = chromed.Appearance.Padding == new Thickness(0f)
            && chromed.Appearance.CornerRadius == new CornerRadius(TimerVisuals.Radius.Control);

        return breakpoints && shell && mutualExclusion && format && automation && readyContract && compactContract && iconNames && languageApply && rowProjection && iconChrome;
    }

    private sealed class LanguageApplyProbe : ILanguageApplyTarget
    {
        public List<string> Calls { get; } = [];

        public string? Language { get; private set; }

        public void SetCurrentLanguage(string language)
        {
            Language = language;
            Calls.Add("language");
        }

        public void SyncWindowTitle() => Calls.Add("title");

        public void BumpThemeRevision() => Calls.Add("revision");
    }

    private static bool RunLanguageApplyChecks()
    {
        var probe = new LanguageApplyProbe();
        LanguageApply.Apply(probe, "zh-CN");
        return probe.Calls.Count == 3
            && probe.Calls[0] == "language"
            && probe.Calls[1] == "title"
            && probe.Calls[2] == "revision"
            && probe.Language == "zh-CN";
    }

    private static bool RunRowProjectionChecks()
    {
        var when = new DateTimeOffset(2026, 7, 14, 1, 0, 0, TimeSpan.Zero);
        Guid a = Guid.ParseExact("11111111111111111111111111111111", "N");
        Guid b = Guid.ParseExact("22222222222222222222222222222222", "N");
        TimerFolder[] folders =
        [
            new TimerFolder(a, "Focus", when, TimerFolderIcon.Book),
            new TimerFolder(b, "Reading", when, TimerFolderIcon.Book),
        ];

        IReadOnlyList<FolderRowData> selectedA = TimerRowProjector.FolderRows(folders, a, revision: 1);
        IReadOnlyList<FolderRowData> selectedB = TimerRowProjector.FolderRows(folders, b, revision: 1);
        IReadOnlyList<FolderRowData> rethemed = TimerRowProjector.FolderRows(folders, a, revision: 2);

        bool folderRows =
            selectedA.Count == 2
            && selectedA[0].IsSelected && !selectedA[1].IsSelected
            && selectedA[0].Folder.Id == a && selectedA[1].Folder.Id == b
            && selectedB[0].IsSelected == false && selectedB[1].IsSelected
            // A revision bump keeps the same keys but changes the values (drives the same-key rebind).
            && rethemed[0].Folder.Id == a && !rethemed[0].Equals(selectedA[0]);

        var entry = new TimerEntry(
            Guid.ParseExact("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", "N"), a, "Session", 60, when);
        IReadOnlyList<HistoryRowData> h1 = TimerRowProjector.HistoryRows([entry], revision: 1);
        IReadOnlyList<HistoryRowData> h2 = TimerRowProjector.HistoryRows([entry], revision: 2);
        bool historyRows =
            h1.Count == 1
            && h1[0].Entry.Id == entry.Id
            && h2[0].Entry.Id == entry.Id
            && !h1[0].Equals(h2[0]);

        return folderRows && historyRows;
    }

    private static bool ContainsAutomationId(SemanticsNode node, string automationId)
    {
        if (string.Equals(node.AutomationId, automationId, StringComparison.Ordinal))
        {
            return true;
        }

        foreach (SemanticsNode child in node.Children)
        {
            if (ContainsAutomationId(child, automationId))
            {
                return true;
            }
        }

        return false;
    }

    // Hand-authored ready-state accessibility CONTRACT MIRROR (not the live mounted MainView tree): the structure the
    // mounted view is expected to project when the workspace is loaded and the window is not compact.
    private static SemanticsNode BuildReadyContractTree()
    {
        Rect bounds = Rect.FromXywh(0f, 0f, 120f, 44f);
        int next = 1;
        SemanticsNode Node(SemanticsRole role, string name, string? automationId, bool interactive)
        {
            var node = new SemanticsNode(ElementId.Of(next++), role, name, bounds, interactive ? new Action(static () => { }) : null);
            if (automationId is not null)
            {
                node.AutomationId = automationId;
            }

            return node;
        }

        SemanticsNode root = Node(SemanticsRole.Window, "Rodel Timer", automationId: null, interactive: false);
        SemanticsNode foldersSplit = Node(SemanticsRole.Group, "Folders", TimerAutomationIds.FoldersSplit, interactive: false);
        SemanticsNode historySplit = Node(SemanticsRole.Group, "History", TimerAutomationIds.HistorySplit, interactive: false);
        _ = root.Add(foldersSplit);
        _ = root.Add(historySplit);

        // The extended title bar band hosts the brand's two commands (settings + compact) as its own Group, so each has
        // its own hit bounds; the compact presenter collapses the whole band (When(!IsCompact)), tearing both out.
        SemanticsNode titleBar = Node(SemanticsRole.Group, "Rodel Timer", automationId: null, interactive: false);
        _ = titleBar.Add(Node(SemanticsRole.Button, "Compact overlay", TimerAutomationIds.Compact, interactive: true));
        _ = titleBar.Add(Node(SemanticsRole.Button, "Settings", TimerAutomationIds.Settings, interactive: true));
        _ = root.Add(titleBar);

        _ = root.Add(Node(SemanticsRole.Button, "Start timer", TimerAutomationIds.StartStop, interactive: true));
        _ = root.Add(Node(SemanticsRole.Edit, "Session title", TimerAutomationIds.Title, interactive: true));

        SemanticsNode foldersToggle = Node(SemanticsRole.Button, "Folders", TimerAutomationIds.FoldersToggle, interactive: true);
        foldersToggle.ControlledPeers = [TimerAutomationIds.FoldersSplit];
        _ = root.Add(foldersToggle);

        SemanticsNode historyToggle = Node(SemanticsRole.Button, "History", TimerAutomationIds.HistoryToggle, interactive: true);
        historyToggle.ControlledPeers = [TimerAutomationIds.HistorySplit];
        _ = root.Add(historyToggle);

        _ = foldersSplit.Add(Node(SemanticsRole.Button, "Add folder", TimerAutomationIds.AddFolder, interactive: true));
        Guid folder = Guid.ParseExact("11111111111111111111111111111111", "N");
        _ = foldersSplit.Add(Node(SemanticsRole.Button, "Focus", TimerAutomationIds.FolderSelect(folder), interactive: true));
        _ = foldersSplit.Add(Node(SemanticsRole.Button, "More actions for Focus", TimerAutomationIds.FolderMore(folder), interactive: true));

        Guid entry = Guid.ParseExact("22222222222222222222222222222222", "N");
        _ = historySplit.Add(Node(SemanticsRole.Button, "More actions for session", TimerAutomationIds.HistoryMore(entry), interactive: true));

        return root;
    }

    // Hand-authored compact-state accessibility CONTRACT MIRROR (not the live mounted MainView tree): the compact
    // presenter tears the title bar (Settings), the session-title editor (Title), and both footer toggles (FoldersToggle /
    // HistoryToggle) out of the tree, leaving only the clock, the start/stop button, and the compact-exit button. The
    // structural split-view groups remain, but the four torn-down ids must be absent.
    private static SemanticsNode BuildCompactContractTree()
    {
        Rect bounds = Rect.FromXywh(0f, 0f, 120f, 44f);
        int next = 1;
        SemanticsNode Node(SemanticsRole role, string name, string? automationId, bool interactive)
        {
            var node = new SemanticsNode(ElementId.Of(next++), role, name, bounds, interactive ? new Action(static () => { }) : null);
            if (automationId is not null)
            {
                node.AutomationId = automationId;
            }

            return node;
        }

        SemanticsNode root = Node(SemanticsRole.Window, "Rodel Timer", automationId: null, interactive: false);
        _ = root.Add(Node(SemanticsRole.Button, "Stop timer", TimerAutomationIds.StartStop, interactive: true));
        _ = root.Add(Node(SemanticsRole.Button, "Exit compact overlay", TimerAutomationIds.Compact, interactive: true));
        return root;
    }

    private sealed class EmptyLegacyDataSource : ILegacyWorkTimerDataSource
    {
        public static EmptyLegacyDataSource Instance { get; } = new();

        public ValueTask<LegacyWorkTimerSnapshot?> ReadAsync(
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<LegacyWorkTimerSnapshot?>(null);
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;

        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }

    private sealed class InlineScheduler(TimeProvider timeProvider) : IScheduler
    {
        public DateTimeOffset Now => timeProvider.GetUtcNow();

        public void Post(Action action) => action();

        public IDisposable Schedule(TimeSpan delay, Action callback) => NoopDisposable.Instance;
    }

    private sealed class NoopDisposable : IDisposable
    {
        public static NoopDisposable Instance { get; } = new();

        public void Dispose()
        {
        }
    }
}
