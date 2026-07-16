using Rodel.Timer.Core.Folders;
using Rodel.Timer.Core.History;
using Rodel.Timer.Presentation;

using Sprout;
using Sprout.Controls;
using Sprout.Graphics;
using Sprout.Input;
using Sprout.Layout;
using Sprout.Popups;
using Sprout.Semantics;
using Sprout.Theming;
using Sprout.Widgets;

using static Sprout.Markup;

namespace Rodel.Timer.Tests.Accessibility;

/// <summary>Mounted product projections for the folder/history ListItem context-menu contract.</summary>
[TestClass]
public sealed class RowContextProjectionTests
{
    private static readonly Color Primary = new(240, 240, 240, 255);
    private static readonly Size Viewport = new(336f, 240f);

    [TestMethod]
    public void FolderList_MountsSelectableRowsWithFreshContextMenus()
    {
        Guid firstId = Guid.ParseExact("11111111111111111111111111111111", "N");
        Guid secondId = Guid.ParseExact("22222222222222222222222222222222", "N");
        FolderRowData[] rows =
        [
            new(new TimerFolder(firstId, "Focus", DateTimeOffset.UnixEpoch, TimerFolderIcon.Folder), true, 0),
            new(new TimerFolder(secondId, "Reading", DateTimeOffset.UnixEpoch, TimerFolderIcon.Book), false, 0),
        ];
        var builds = 0;
        using ItemsView mounted = Mount(TimerRowListFactory.Folders(
            rows,
            static _ => Rectangle(Primary, new Size(300f, 64f)),
            new StackLayout { ItemExtent = 72f, Spacing = 4f },
            data => new AutomationInfo
            {
                AutomationId = TimerAutomationIds.FolderSelect(data.Folder.Id),
                ItemStatus = data.IsSelected ? "Current folder" : null,
                HelpText = "Open the context menu for Edit and Delete",
            },
            data =>
            {
                builds++;
                return new MenuFlyout(
                    new MenuFlyoutItem("Edit"),
                    new MenuFlyoutItem("Delete"))
                    .Automation(new AutomationInfo
                    {
                        AutomationId = TimerAutomationIds.FolderMenu(data.Folder.Id),
                    });
            },
            [0],
            static _ => { }));

        SemanticsNode listNode = FindRole(mounted.Semantics!, SemanticsRole.List)!;
        SemanticsNode[] items = listNode.Children.Where(static node => node.Role == SemanticsRole.ListItem).ToArray();
        ItemContainerInteraction[] interactions = FindItemInteractions(mounted.Interaction!).ToArray();

        Assert.IsTrue(listNode.IsSelectionContainer);
        Assert.HasCount(2, items);
        Assert.IsTrue(items[0].SupportsSelectionItem);
        Assert.AreEqual(TimerAutomationIds.FolderSelect(firstId), items[0].AutomationId);
        Assert.HasCount(2, interactions);
        Assert.IsTrue(interactions.All(static item => item.Focusable && item.ContextFlyout is not null));

        IFlyout first = interactions[0].ContextFlyout!();
        IFlyout second = interactions[0].ContextFlyout!();
        Assert.AreNotSame(first, second);
        Assert.AreEqual(2, builds);
    }

    [TestMethod]
    public void HistoryList_MountsHonestFocusableRowsAndRebuildsDynamicMenuState()
    {
        Guid folderId = Guid.ParseExact("11111111111111111111111111111111", "N");
        Guid entryId = Guid.ParseExact("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", "N");
        HistoryRowData[] rows =
        [
            new(new TimerEntry(entryId, folderId, "Deep work", 300, DateTimeOffset.UnixEpoch), 0),
        ];
        bool canMove = false;
        using ItemsView mounted = Mount(TimerRowListFactory.History(
            rows,
            static _ => Rectangle(Primary, new Size(300f, 72f)),
            new StackLayout { ItemExtent = 76f, Spacing = 4f },
            data => new AutomationInfo
            {
                AutomationId = TimerAutomationIds.HistoryItem(data.Entry.Id),
                HelpText = "Open the context menu for Move and Delete",
            },
            data => new MenuFlyout(
                new MenuFlyoutItem("Move") { IsEnabled = canMove },
                new MenuFlyoutItem("Delete"))
                .Automation(new AutomationInfo
                {
                    AutomationId = TimerAutomationIds.HistoryMenu(data.Entry.Id),
                })));

        SemanticsNode listNode = FindRole(mounted.Semantics!, SemanticsRole.List)!;
        SemanticsNode item = listNode.Children.Single(static node => node.Role == SemanticsRole.ListItem);
        ItemContainerInteraction interaction = FindItemInteractions(mounted.Interaction!).Single();

        Assert.IsFalse(listNode.IsSelectionContainer);
        Assert.IsFalse(item.SupportsSelectionItem);
        Assert.AreEqual(TimerAutomationIds.HistoryItem(entryId), item.AutomationId);
        Assert.IsTrue(interaction.Focusable);
        Assert.IsNotNull(interaction.ContextFlyout);

        var first = (MenuFlyout)interaction.ContextFlyout();
        Assert.IsFalse(((MenuFlyoutItem)first.Items[0]).IsEnabled);
        canMove = true;
        var second = (MenuFlyout)interaction.ContextFlyout();
        Assert.AreNotSame(first, second);
        Assert.IsTrue(((MenuFlyoutItem)second.Items[0]).IsEnabled);
    }

    private static ItemsView Mount(ItemsView view)
    {
        using Sprout.Theming.Environment.Scope theme = Sprout.Theming.Environment.Push(
            Theme.Key,
            new Theme(SystemThemeSnapshot.Default));
        view.SetRootId(ElementId.Of(1));
        view.SetInvalidationSink(static () => { });
        view.Mount();
        LayoutPass.Run(view.Render, BoxConstraints.Tight(Viewport), new LayoutContext(null, 0f));
        view.SyncGeometry();
        return view;
    }

    private static IEnumerable<ItemContainerInteraction> FindItemInteractions(InteractionNode node)
    {
        if (node is ItemContainerInteraction item)
        {
            yield return item;
        }

        foreach (InteractionNode child in node.Children)
        {
            foreach (ItemContainerInteraction descendant in FindItemInteractions(child))
            {
                yield return descendant;
            }
        }
    }

    private static SemanticsNode? FindRole(SemanticsNode node, SemanticsRole role)
    {
        if (node.Role == role)
        {
            return node;
        }

        foreach (SemanticsNode child in node.Children)
        {
            if (FindRole(child, role) is { } match)
            {
                return match;
            }
        }

        return null;
    }
}
