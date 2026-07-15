using Rodel.Timer.Presentation;

using Sprout.Graphics;
using Sprout.Semantics;

namespace Rodel.Timer.Tests.Accessibility;

/// <summary>
/// Headless accessibility-audit gate for the ready (loaded) and compact shells. It assembles hand-authored
/// <b>contract mirrors</b> — <see cref="SemanticsNode"/> trees in the shape the mounted UI is expected to project, built
/// from the real <see cref="TimerAutomationIds"/> builders — <em>not</em> the live mounted tree (that is verified
/// end-to-end by the Sprout.DevTools drive over the published exe). The ready mirror carries the two split-view groups,
/// the header/centre/footer command controls (each named + carrying its stable <c>RodelTimer.*</c> automation id), the
/// pane toggles whose <c>ControlledPeers</c> point at the split groups, and per-folder / per-entry rows keyed by
/// <see cref="Guid"/>. The compact mirror models the compact overlay presenter, which tears the header (Settings), the
/// session-title editor (Title), and both footer toggles out of the tree. A clean audit on each proves no unnamed
/// control, no duplicate element, and no dangling relation; the id-presence assertions pin exactly which controls each
/// state exposes so a screen reader / UI-test driver addresses them unambiguously.
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

    private static SemanticsNode BuildReadyTree(out List<string> rowAutomationIds)
    {
        var builder = new TreeBuilder();
        rowAutomationIds = [];

        SemanticsNode root = builder.Node(SemanticsRole.Window, "Rodel Timer", automationId: null, interactive: false);

        SemanticsNode foldersSplit = builder.Node(SemanticsRole.Group, "Folders", TimerAutomationIds.FoldersSplit, interactive: false);
        SemanticsNode historySplit = builder.Node(SemanticsRole.Group, "History", TimerAutomationIds.HistorySplit, interactive: false);
        _ = root.Add(foldersSplit);
        _ = root.Add(historySplit);

        _ = root.Add(builder.Node(SemanticsRole.Button, "Settings", TimerAutomationIds.Settings, interactive: true));
        _ = root.Add(builder.Node(SemanticsRole.Button, "Start timer", TimerAutomationIds.StartStop, interactive: true));
        _ = root.Add(builder.Node(SemanticsRole.Edit, "Session title", TimerAutomationIds.Title, interactive: true));
        _ = root.Add(builder.Node(SemanticsRole.Button, "Compact overlay", TimerAutomationIds.Compact, interactive: true));

        SemanticsNode foldersToggle = builder.Node(SemanticsRole.Button, "Folders", TimerAutomationIds.FoldersToggle, interactive: true);
        foldersToggle.ControlledPeers = [TimerAutomationIds.FoldersSplit];
        _ = root.Add(foldersToggle);

        SemanticsNode historyToggle = builder.Node(SemanticsRole.Button, "History", TimerAutomationIds.HistoryToggle, interactive: true);
        historyToggle.ControlledPeers = [TimerAutomationIds.HistorySplit];
        _ = root.Add(historyToggle);

        _ = foldersSplit.Add(builder.Node(SemanticsRole.Button, "Add folder", TimerAutomationIds.AddFolder, interactive: true));

        for (int i = 0; i < Folders.Length; i++)
        {
            string select = TimerAutomationIds.FolderSelect(Folders[i]);
            string more = TimerAutomationIds.FolderMore(Folders[i]);
            rowAutomationIds.Add(select);
            rowAutomationIds.Add(more);
            _ = foldersSplit.Add(builder.Node(SemanticsRole.Button, $"Folder {i}", select, interactive: true));
            _ = foldersSplit.Add(builder.Node(SemanticsRole.Button, $"More actions for folder {i}", more, interactive: true));
        }

        for (int i = 0; i < Entries.Length; i++)
        {
            string more = TimerAutomationIds.HistoryMore(Entries[i]);
            rowAutomationIds.Add(more);
            _ = historySplit.Add(builder.Node(SemanticsRole.Button, $"More actions for session {i}", more, interactive: true));
        }

        return root;
    }

    [TestMethod]
    public void ReadyTree_HasNoAccessibilityViolations()
    {
        SemanticsNode root = BuildReadyTree(out _);

        IReadOnlyList<AccessibilityViolation> violations = AccessibilityAudit.Audit(root);

        Assert.IsEmpty(
            violations,
            $"the ready tree must be clean; found: {string.Join(", ", violations.Select(static v => $"{v.Rule} @ {v.Element}"))}");
    }

    [TestMethod]
    public void ReadyTree_EveryRowAutomationIdIsUnique()
    {
        _ = BuildReadyTree(out List<string> rowAutomationIds);

        Assert.AreEqual(
            rowAutomationIds.Count,
            rowAutomationIds.Distinct(StringComparer.Ordinal).Count(),
            "every folder/history row id must be unique.");
    }

    /// <summary>The automation ids the compact overlay presenter tears down (header settings, title, both toggles).</summary>
    private static readonly string[] CompactOmittedIds =
    [
        TimerAutomationIds.Settings,
        TimerAutomationIds.Title,
        TimerAutomationIds.FoldersToggle,
        TimerAutomationIds.HistoryToggle,
    ];

    // The compact CONTRACT MIRROR: only the clock (non-interactive, no id), the start/stop button, and the compact-exit
    // button survive; the header, title editor, and both footer toggles are torn out of the tree.
    private static SemanticsNode BuildCompactTree()
    {
        var builder = new TreeBuilder();
        SemanticsNode root = builder.Node(SemanticsRole.Window, "Rodel Timer", automationId: null, interactive: false);
        _ = root.Add(builder.Node(SemanticsRole.Button, "Stop timer", TimerAutomationIds.StartStop, interactive: true));
        _ = root.Add(builder.Node(SemanticsRole.Button, "Exit compact overlay", TimerAutomationIds.Compact, interactive: true));
        return root;
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

    [TestMethod]
    public void ReadyTree_ExposesEveryControlTheCompactPresenterTearsDown()
    {
        SemanticsNode root = BuildReadyTree(out _);

        foreach (string id in CompactOmittedIds)
        {
            Assert.IsTrue(ContainsAutomationId(root, id), $"the ready tree must expose '{id}'.");
        }
    }

    [TestMethod]
    public void CompactTree_HasNoAccessibilityViolations()
    {
        SemanticsNode root = BuildCompactTree();

        IReadOnlyList<AccessibilityViolation> violations = AccessibilityAudit.Audit(root);

        Assert.IsEmpty(
            violations,
            $"the compact tree must be clean; found: {string.Join(", ", violations.Select(static v => $"{v.Rule} @ {v.Element}"))}");
    }

    [TestMethod]
    public void CompactTree_OmitsSettingsTitleAndBothToggles()
    {
        SemanticsNode root = BuildCompactTree();

        foreach (string id in CompactOmittedIds)
        {
            Assert.IsFalse(
                ContainsAutomationId(root, id),
                $"the compact tree must not expose '{id}' (it is torn down in the compact presenter).");
        }
    }

    [TestMethod]
    public void CompactTree_KeepsTheCoreTimerEssentials()
    {
        SemanticsNode root = BuildCompactTree();

        Assert.IsTrue(ContainsAutomationId(root, TimerAutomationIds.StartStop), "compact must keep the start/stop button.");
        Assert.IsTrue(ContainsAutomationId(root, TimerAutomationIds.Compact), "compact must keep the compact-exit button.");
    }

    [TestMethod]
    public void RemovingATogglesControlledSplit_IsReportedAsADanglingRelation()
    {
        // Sanity-check that the audit actually exercises the ControlledPeers relation we rely on: if a toggle points
        // at a split id that is not present in the tree, the audit must flag it as a dangling relation.
        var builder = new TreeBuilder();
        SemanticsNode root = builder.Node(SemanticsRole.Window, "Rodel Timer", automationId: null, interactive: false);
        SemanticsNode toggle = builder.Node(SemanticsRole.Button, "Folders", TimerAutomationIds.FoldersToggle, interactive: true);
        toggle.ControlledPeers = [TimerAutomationIds.FoldersSplit];
        _ = root.Add(toggle);

        IReadOnlyList<AccessibilityViolation> violations = AccessibilityAudit.Audit(root);

        Assert.IsTrue(
            violations.Any(static v => v.Rule == AccessibilityRule.DanglingRelation),
            "a toggle controlling a missing split must be a dangling relation.");
    }
}
