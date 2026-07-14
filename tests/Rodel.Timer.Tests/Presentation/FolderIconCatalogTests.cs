using Rodel.Timer.Core.Folders;
using Rodel.Timer.Presentation;

namespace Rodel.Timer.Tests.Presentation;

[TestClass]
public sealed class FolderIconCatalogTests
{
    [TestMethod]
    public void Catalog_CoversEveryLegacyFolderIconExactlyOnce()
    {
        TimerFolderIcon[] expected = Enum.GetValues<TimerFolderIcon>();

        Assert.HasCount(expected.Length, FolderIconCatalog.All);
        Assert.HasCount(
            expected.Length,
            FolderIconCatalog.All.Select(static option => option.Icon).Distinct());

        foreach (TimerFolderIcon icon in expected)
        {
            Assert.IsTrue(
                FolderIconCatalog.All.Any(option => option.Icon == icon),
                $"Missing Fluent icon mapping for {icon}.");
        }
    }

    [TestMethod]
    public void Resolve_ReturnsTheMappedSymbol()
    {
        foreach (FolderIconOption option in FolderIconCatalog.All)
        {
            Assert.AreEqual(option.Symbol, FolderIconCatalog.Resolve(option.Icon));
        }
    }
}
