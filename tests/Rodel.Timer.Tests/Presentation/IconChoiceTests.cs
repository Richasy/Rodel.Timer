using Rodel.Timer.Core.Folders;
using Rodel.Timer.Presentation;

namespace Rodel.Timer.Tests.Presentation;

/// <summary>
/// Unit coverage of <see cref="FolderIconCatalog.Choices"/> — the pure icon-picker choice list the folder editor
/// rebuilds on every render. The list must mirror the catalog in order, flag exactly the selected icon, and — the
/// crux of the focus-preserving fix — keep the <b>same keys in the same order</b> across a selection change, so the
/// icon-keyed repeater rebinds only the two cells whose <see cref="IconChoice.IsSelected"/> flipped instead of
/// remounting the whole grid (which lost keyboard focus and scroll position).
/// </summary>
[TestClass]
public sealed class IconChoiceTests
{
    [TestMethod]
    public void Choices_MirrorsTheCatalogOrderAndSymbols()
    {
        IReadOnlyList<IconChoice> choices = FolderIconCatalog.Choices(TimerFolderIcon.Folder, 0);

        Assert.HasCount(FolderIconCatalog.All.Count, choices);
        for (int i = 0; i < choices.Count; i++)
        {
            Assert.AreEqual(FolderIconCatalog.All[i].Icon, choices[i].Icon);
            Assert.AreEqual(FolderIconCatalog.All[i].Symbol, choices[i].Symbol);
        }
    }

    [TestMethod]
    public void Choices_FlagsExactlyTheSelectedIcon()
    {
        IReadOnlyList<IconChoice> choices = FolderIconCatalog.Choices(TimerFolderIcon.Book, 0);

        IEnumerable<IconChoice> selected = choices.Where(static c => c.IsSelected);
        Assert.HasCount(1, selected);
        Assert.AreEqual(TimerFolderIcon.Book, selected.Single().Icon);
    }

    [TestMethod]
    public void Choices_ForAnIconOutsideTheCatalog_FlagsNothing()
    {
        // A value never present in the picker catalog must leave every cell unselected (no false highlight).
        IReadOnlyList<IconChoice> choices = FolderIconCatalog.Choices((TimerFolderIcon)int.MaxValue, 0);

        Assert.IsFalse(choices.Any(static c => c.IsSelected));
    }

    [TestMethod]
    public void Choices_KeepTheSameKeysInTheSameOrderAcrossASelectionChange()
    {
        // The repeater keys cells by TimerFolderIcon: a selection change must not reorder or replace the key set,
        // otherwise the repeater would remount cells and lose focus/scroll. Only the IsSelected flags may move.
        IReadOnlyList<IconChoice> before = FolderIconCatalog.Choices(TimerFolderIcon.Folder, 0);
        IReadOnlyList<IconChoice> after = FolderIconCatalog.Choices(TimerFolderIcon.Book, 0);

        CollectionAssert.AreEqual(
            before.Select(static c => c.Icon).ToArray(),
            after.Select(static c => c.Icon).ToArray());

        Assert.AreEqual(TimerFolderIcon.Folder, before.Single(static c => c.IsSelected).Icon);
        Assert.AreEqual(TimerFolderIcon.Book, after.Single(static c => c.IsSelected).Icon);
    }

    [TestMethod]
    public void Choices_StampsEveryChoiceWithTheRevision()
    {
        IReadOnlyList<IconChoice> choices = FolderIconCatalog.Choices(TimerFolderIcon.Folder, 7);

        Assert.IsTrue(choices.All(static c => c.Revision == 7));
    }

    [TestMethod]
    public void Choices_ARevisionBumpReValuesEveryCellAtTheSameKey()
    {
        // A theme/language switch changes neither the selection nor any symbol, so without the revision the choices
        // would compare equal and the same-key rebind would no-op (freezing stale colours + names). Folding the
        // revision into value equality makes every unchanged icon key produce a NEW value, forcing the rebind — while
        // the key set (and therefore grid/scroll/focus identity) is untouched.
        IReadOnlyList<IconChoice> before = FolderIconCatalog.Choices(TimerFolderIcon.Book, 0);
        IReadOnlyList<IconChoice> after = FolderIconCatalog.Choices(TimerFolderIcon.Book, 1);

        CollectionAssert.AreEqual(
            before.Select(static c => c.Icon).ToArray(),
            after.Select(static c => c.Icon).ToArray());

        for (int i = 0; i < before.Count; i++)
        {
            Assert.AreEqual(before[i].Icon, after[i].Icon);
            Assert.AreEqual(before[i].IsSelected, after[i].IsSelected);
            Assert.AreNotEqual(before[i], after[i]);
        }
    }
}
