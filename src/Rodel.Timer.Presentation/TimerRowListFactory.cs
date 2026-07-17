using Sprout.Controls;
using Sprout.Popups;
using Sprout.Reconcile;
using Sprout.Semantics;
using Sprout.Widgets;

using static Sprout.Markup;

namespace Rodel.Timer.Presentation;

/// <summary>
/// Builds the product's folder/history ItemsView configurations so app hosting and mounted tests share the exact
/// selection, focus, identity, automation, and context-menu policy.
/// </summary>
public static class TimerRowListFactory
{
    /// <summary>Builds the controlled single-select folder list.</summary>
    public static ItemsView Folders<TView>(
        IReadOnlyList<FolderRowData> rows,
        Func<FolderRowData, TView> template,
        VirtualizingLayout layout,
        Func<FolderRowData, AutomationInfo> itemAutomation,
        Func<FolderRowData, IFlyout> itemContextFlyout,
        IReadOnlyList<int> selectedIndices,
        Action<IReadOnlyList<int>> onSelectionChanged)
        where TView : IView =>
        ItemsView(
            rows,
            static data => data.Folder.Id,
            template,
            layout,
            accessibleName: static data => data.Folder.Name,
            itemAutomation: itemAutomation,
            itemContextFlyout: itemContextFlyout)
        .SelectionMode(SelectionMode.Single)
        .SelectedIndices(selectedIndices)
        .OnSelectionChanged(onSelectionChanged)
        .SingleSelectionFollowsFocus(false);

    /// <summary>Builds the honest non-selecting, focusable history list. When <paramref name="onItemInvoked"/> is supplied a primary click / Enter / Space invokes the row (opening its detail) without selecting it.</summary>
    public static ItemsView History<TView>(
        IReadOnlyList<HistoryRowData> rows,
        Func<HistoryRowData, TView> template,
        VirtualizingLayout layout,
        Func<HistoryRowData, AutomationInfo> itemAutomation,
        Func<HistoryRowData, IFlyout> itemContextFlyout,
        Action<int>? onItemInvoked = null)
        where TView : IView
    {
        ItemsView view = ItemsView(
            rows,
            static data => data.Entry.Id,
            template,
            layout,
            accessibleName: static data => data.Entry.Title,
            itemAutomation: itemAutomation,
            itemContextFlyout: itemContextFlyout)
        .SelectionMode(SelectionMode.None);

        if (onItemInvoked is not null)
        {
            view.OnItemInvoked(onItemInvoked);
        }

        return view;
    }
}
