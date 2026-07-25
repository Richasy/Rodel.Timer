using Rodel.Timer.Presentation;

using Sprout.Graphics;
using Sprout.Semantics;

namespace Rodel.Timer.Tests.Accessibility;

/// <summary>
/// Headless contract mirrors for the closed normal shell, each anchored surface, a nested row context menu, and compact
/// mode. Real mounted UIA evidence remains a DevTools gate; these mirrors cheaply pin identity and absence invariants.
/// </summary>
[TestClass]
public sealed class ReadyTreeAccessibilityAuditTests
{
    private static readonly Rect Bounds = Rect.FromXywh(0f, 0f, 120f, 44f);

    private static readonly Guid[] Folders =
    [
        new("11111111-1111-1111-1111-111111111111"),
        new("22222222-2222-2222-2222-222222222222"),
        new("33333333-3333-3333-3333-333333333333"),
    ];

    private static readonly Guid[] Entries =
    [
        new("aaaaaaaa-0000-0000-0000-000000000001"),
        new("bbbbbbbb-0000-0000-0000-000000000002"),
    ];

    private sealed class TreeBuilder
    {
        private int _nextId = 1;

        public SemanticsNode Node(SemanticsRole role, string name, string? automationId, bool interactive)
        {
            var node = new SemanticsNode(ElementId.Of(_nextId++), role, name, Bounds, interactive ? Noop : null);
            if (automationId is not null)
            {
                node.AutomationId = automationId;
            }

            return node;
        }

        private static void Noop()
        {
        }
    }

    private static SemanticsNode BuildClosedNormalTree(
        TreeBuilder? builder = null,
        bool isPinned = false,
        bool isMaximized = false,
        bool isSidePanelLayout = false)
    {
        builder ??= new TreeBuilder();
        TimerWindowPinBehavior pinBehavior = TimerWindowPinPolicy.Resolve(isPinned, isMaximized);
        SemanticsNode root = builder.Node(SemanticsRole.Window, "Rodel Timer", automationId: null, interactive: false);
        if (!isSidePanelLayout)
        {
            _ = root.Add(builder.Node(SemanticsRole.Button, "Folders", TimerAutomationIds.FoldersToggle, interactive: true));
            _ = root.Add(builder.Node(SemanticsRole.Button, "History", TimerAutomationIds.HistoryToggle, interactive: true));
        }

        if (pinBehavior.ShowCompactOverlayButton)
        {
            _ = root.Add(builder.Node(SemanticsRole.Button, "Compact overlay", TimerAutomationIds.Compact, interactive: true));
        }

        if (pinBehavior.ShowPinButton)
        {
            _ = root.Add(builder.Node(SemanticsRole.Button, isPinned ? "Unpin window" : "Pin window", TimerAutomationIds.PinWindow, interactive: true));
        }

        _ = root.Add(builder.Node(SemanticsRole.Button, "Settings", TimerAutomationIds.Settings, interactive: true));
        _ = root.Add(builder.Node(SemanticsRole.Button, "Start timer", TimerAutomationIds.StartStop, interactive: true));
        _ = root.Add(builder.Node(SemanticsRole.Edit, "Session title", TimerAutomationIds.Title, interactive: true));
        if (isSidePanelLayout)
        {
            _ = root.Add(builder.Node(SemanticsRole.Button, "Folders", TimerAutomationIds.FoldersPanelToggle, interactive: true));
            _ = root.Add(builder.Node(SemanticsRole.Button, "History", TimerAutomationIds.HistoryPanelToggle, interactive: true));
        }

        return root;
    }

    private static SemanticsNode BuildFoldersPanelOpenTree(out List<string> rowIds)
    {
        var builder = new TreeBuilder();
        SemanticsNode root = BuildClosedNormalTree(builder, isSidePanelLayout: true);
        SemanticsNode panel = builder.Node(SemanticsRole.Pane, "Folders", TimerAutomationIds.FoldersPanel, interactive: false);
        SemanticsNode list = builder.Node(SemanticsRole.List, "Folders", automationId: null, interactive: false);
        _ = root.Add(panel);
        _ = panel.Add(builder.Node(SemanticsRole.Button, "Add folder", TimerAutomationIds.AddFolder, interactive: true));
        _ = panel.Add(list);

        rowIds = [];
        for (int i = 0; i < Folders.Length; i++)
        {
            string id = TimerAutomationIds.FolderSelect(Folders[i]);
            rowIds.Add(id);
            _ = list.Add(builder.Node(SemanticsRole.ListItem, $"Folder {i}", id, interactive: true));
        }

        return root;
    }

    private static SemanticsNode BuildFoldersOpenTree(out List<string> rowIds)
    {
        var builder = new TreeBuilder();
        SemanticsNode root = BuildClosedNormalTree(builder);
        SemanticsNode flyout = builder.Node(SemanticsRole.Pane, "Folders", TimerAutomationIds.FoldersFlyout, interactive: false);
        SemanticsNode content = builder.Node(SemanticsRole.Group, "Folders", TimerAutomationIds.FoldersFlyoutContent, interactive: false);
        SemanticsNode list = builder.Node(SemanticsRole.List, "Folders", automationId: null, interactive: false);
        _ = root.Add(flyout);
        _ = flyout.Add(content);
        _ = content.Add(builder.Node(SemanticsRole.Button, "Add folder", TimerAutomationIds.AddFolder, interactive: true));
        _ = content.Add(list);

        rowIds = [];
        for (int i = 0; i < Folders.Length; i++)
        {
            string id = TimerAutomationIds.FolderSelect(Folders[i]);
            rowIds.Add(id);
            _ = list.Add(builder.Node(SemanticsRole.ListItem, $"Folder {i}", id, interactive: true));
        }

        return root;
    }

    private static SemanticsNode BuildHistoryContextTree(out List<string> rowAndMenuIds)
    {
        var builder = new TreeBuilder();
        SemanticsNode root = BuildClosedNormalTree(builder);
        SemanticsNode flyout = builder.Node(SemanticsRole.Pane, "History", TimerAutomationIds.HistoryFlyout, interactive: false);
        SemanticsNode content = builder.Node(SemanticsRole.Group, "History", TimerAutomationIds.HistoryFlyoutContent, interactive: false);
        SemanticsNode list = builder.Node(SemanticsRole.List, "History", automationId: null, interactive: false);
        _ = root.Add(flyout);
        _ = flyout.Add(content);
        _ = content.Add(list);

        rowAndMenuIds = [];
        for (int i = 0; i < Entries.Length; i++)
        {
            string id = TimerAutomationIds.HistoryItem(Entries[i]);
            rowAndMenuIds.Add(id);
            _ = list.Add(builder.Node(SemanticsRole.ListItem, $"Session {i}", id, interactive: true));
        }

        Guid entry = Entries[0];
        SemanticsNode menu = builder.Node(SemanticsRole.Menu, "Session 0 actions", TimerAutomationIds.HistoryMenu(entry), interactive: false);
        _ = root.Add(menu);
        string move = TimerAutomationIds.HistoryMove(entry);
        string delete = TimerAutomationIds.HistoryDelete(entry);
        rowAndMenuIds.Add(TimerAutomationIds.HistoryMenu(entry));
        rowAndMenuIds.Add(move);
        rowAndMenuIds.Add(delete);
        _ = menu.Add(builder.Node(SemanticsRole.MenuItem, "Move session", move, interactive: true));
        _ = menu.Add(builder.Node(SemanticsRole.MenuItem, "Delete session", delete, interactive: true));
        return root;
    }

    private static SemanticsNode BuildCompactTree()
    {
        var builder = new TreeBuilder();
        SemanticsNode root = builder.Node(SemanticsRole.Window, "Rodel Timer", automationId: null, interactive: false);
        _ = root.Add(builder.Node(SemanticsRole.Button, "Stop timer", TimerAutomationIds.StartStop, interactive: true));
        _ = root.Add(builder.Node(SemanticsRole.Button, "Exit compact overlay", TimerAutomationIds.Compact, interactive: true));
        return root;
    }

    private static readonly string[] CompactOmittedIds =
    [
        TimerAutomationIds.Settings,
        TimerAutomationIds.Title,
        TimerAutomationIds.FoldersToggle,
        TimerAutomationIds.HistoryToggle,
        TimerAutomationIds.FoldersPanelToggle,
        TimerAutomationIds.HistoryPanelToggle,
        TimerAutomationIds.PinWindow,
    ];

    [TestMethod]
    public void ClosedNormalTree_IsCleanAndContainsNoFlyoutDescendants()
    {
        SemanticsNode root = BuildClosedNormalTree();

        AssertAuditClean(root);
        Assert.IsFalse(ContainsAutomationId(root, TimerAutomationIds.FoldersFlyout));
        Assert.IsFalse(ContainsAutomationId(root, TimerAutomationIds.HistoryFlyout));
        Assert.IsFalse(ContainsAutomationId(root, TimerAutomationIds.FolderSelect(Folders[0])));
        Assert.IsFalse(ContainsAutomationId(root, TimerAutomationIds.HistoryItem(Entries[0])));
        Assert.IsTrue(ContainsAutomationId(root, TimerAutomationIds.PinWindow));
    }

    [TestMethod]
    public void PinnedNormalTree_KeepsPinToggleAndOmitsCompactOverlay()
    {
        SemanticsNode root = BuildClosedNormalTree(isPinned: true);

        AssertAuditClean(root);
        Assert.IsTrue(ContainsAutomationId(root, TimerAutomationIds.PinWindow));
        Assert.IsFalse(ContainsAutomationId(root, TimerAutomationIds.Compact));
    }

    [TestMethod]
    public void MaximizedNormalTree_OmitsPinToggle()
    {
        SemanticsNode root = BuildClosedNormalTree(isMaximized: true);

        AssertAuditClean(root);
        Assert.IsFalse(ContainsAutomationId(root, TimerAutomationIds.PinWindow));
        Assert.IsTrue(ContainsAutomationId(root, TimerAutomationIds.Compact));
    }

    [TestMethod]
    public void FoldersOpenTree_IsCleanAndRowsAreUnique()
    {
        SemanticsNode root = BuildFoldersOpenTree(out List<string> rowIds);

        AssertAuditClean(root);
        Assert.AreEqual(rowIds.Count, rowIds.Distinct(StringComparer.Ordinal).Count());
        Assert.IsTrue(ContainsAutomationId(root, TimerAutomationIds.FoldersFlyoutContent));
        Assert.IsFalse(ContainsAutomationId(root, TimerAutomationIds.HistoryFlyout));
    }

    [TestMethod]
    public void HistoryContextTree_PreservesParentAndIsClean()
    {
        SemanticsNode root = BuildHistoryContextTree(out List<string> ids);

        AssertAuditClean(root);
        Assert.AreEqual(ids.Count, ids.Distinct(StringComparer.Ordinal).Count());
        Assert.IsTrue(ContainsAutomationId(root, TimerAutomationIds.HistoryFlyout));
        Assert.IsTrue(ContainsAutomationId(root, TimerAutomationIds.HistoryMenu(Entries[0])));
    }

    [TestMethod]
    public void SidePanelLayoutTree_MovesTheListCommandsToTheFooterWithoutDuplicatingThem()
    {
        SemanticsNode root = BuildClosedNormalTree(isSidePanelLayout: true);

        AssertAuditClean(root);
        Assert.IsFalse(
            ContainsAutomationId(root, TimerAutomationIds.FoldersToggle),
            "the title-bar drop-downs are torn out so the footer commands are the single entry point.");
        Assert.IsFalse(ContainsAutomationId(root, TimerAutomationIds.HistoryToggle));
        Assert.IsTrue(ContainsAutomationId(root, TimerAutomationIds.FoldersPanelToggle));
        Assert.IsTrue(ContainsAutomationId(root, TimerAutomationIds.HistoryPanelToggle));
        Assert.IsFalse(ContainsAutomationId(root, TimerAutomationIds.FoldersPanel), "a closed panel leaves no landmark behind.");
    }

    [TestMethod]
    public void FoldersPanelOpenTree_IsCleanAndRowsAreUnique()
    {
        SemanticsNode root = BuildFoldersPanelOpenTree(out List<string> rowIds);

        AssertAuditClean(root);
        Assert.AreEqual(rowIds.Count, rowIds.Distinct(StringComparer.Ordinal).Count());
        Assert.IsTrue(ContainsAutomationId(root, TimerAutomationIds.FoldersPanel));
        Assert.IsFalse(ContainsAutomationId(root, TimerAutomationIds.HistoryPanel), "only one panel is ever open.");
    }

    [TestMethod]
    public void CompactTree_IsCleanAndOmitsNormalOnlyControls()
    {
        SemanticsNode root = BuildCompactTree();

        AssertAuditClean(root);
        foreach (string id in CompactOmittedIds)
        {
            Assert.IsFalse(ContainsAutomationId(root, id));
        }

        Assert.IsTrue(ContainsAutomationId(root, TimerAutomationIds.StartStop));
        Assert.IsTrue(ContainsAutomationId(root, TimerAutomationIds.Compact));
    }

    private static void AssertAuditClean(SemanticsNode root)
    {
        IReadOnlyList<AccessibilityViolation> violations = AccessibilityAudit.Audit(root);
        Assert.IsEmpty(
            violations,
            $"the semantics contract must be clean; found: {string.Join(", ", violations.Select(static violation => $"{violation.Rule} @ {violation.Element}"))}");
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
}
