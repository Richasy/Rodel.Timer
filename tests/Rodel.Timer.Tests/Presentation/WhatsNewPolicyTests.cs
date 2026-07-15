using Rodel.Timer.Core.Bootstrap;
using Rodel.Timer.Presentation;

namespace Rodel.Timer.Tests.Presentation;

/// <summary>
/// Unit coverage of <see cref="WhatsNewPolicy.Decide"/> — the pure decision that keeps the "what's new" upgrade notice
/// from ever appearing on a brand-new install. A fresh <see cref="TimerBootstrapOrigin.Created"/> workspace only records
/// the current version (<see cref="WhatsNewAction.RecordOnly"/>); an existing or migrated workspace shows the notice
/// (<see cref="WhatsNewAction.Show"/>) only when its last-seen version differs from the current one.
/// </summary>
[TestClass]
public sealed class WhatsNewPolicyTests
{
    private const string Current = "2.0.0";

    [TestMethod]
    public void Created_WithNoLastSeenVersion_RecordsOnly()
    {
        Assert.AreEqual(WhatsNewAction.RecordOnly, WhatsNewPolicy.Decide(TimerBootstrapOrigin.Created, null, Current));
    }

    [TestMethod]
    public void Created_WithAnOlderLastSeenVersion_RecordsOnly()
    {
        Assert.AreEqual(WhatsNewAction.RecordOnly, WhatsNewPolicy.Decide(TimerBootstrapOrigin.Created, "1.0.0", Current));
    }

    [TestMethod]
    public void Created_WhenAlreadyAtCurrentVersion_DoesNothing()
    {
        Assert.AreEqual(WhatsNewAction.None, WhatsNewPolicy.Decide(TimerBootstrapOrigin.Created, Current, Current));
    }

    [TestMethod]
    public void Existing_WithNoLastSeenVersion_Shows()
    {
        Assert.AreEqual(WhatsNewAction.Show, WhatsNewPolicy.Decide(TimerBootstrapOrigin.Existing, null, Current));
    }

    [TestMethod]
    public void Existing_WithAnOlderLastSeenVersion_Shows()
    {
        Assert.AreEqual(WhatsNewAction.Show, WhatsNewPolicy.Decide(TimerBootstrapOrigin.Existing, "1.0.0", Current));
    }

    [TestMethod]
    public void Existing_WhenAlreadyAtCurrentVersion_DoesNothing()
    {
        Assert.AreEqual(WhatsNewAction.None, WhatsNewPolicy.Decide(TimerBootstrapOrigin.Existing, Current, Current));
    }

    [TestMethod]
    public void Migrated_WithAnOlderLastSeenVersion_Shows()
    {
        Assert.AreEqual(WhatsNewAction.Show, WhatsNewPolicy.Decide(TimerBootstrapOrigin.Migrated, "1.0.0", Current));
    }

    [TestMethod]
    public void Migrated_WithNoLastSeenVersion_Shows()
    {
        Assert.AreEqual(WhatsNewAction.Show, WhatsNewPolicy.Decide(TimerBootstrapOrigin.Migrated, null, Current));
    }

    [TestMethod]
    public void Migrated_WhenAlreadyAtCurrentVersion_DoesNothing()
    {
        Assert.AreEqual(WhatsNewAction.None, WhatsNewPolicy.Decide(TimerBootstrapOrigin.Migrated, Current, Current));
    }

    [TestMethod]
    public void Decide_WithNoCurrentVersion_Throws()
    {
        _ = Assert.Throws<ArgumentException>(
            () => WhatsNewPolicy.Decide(TimerBootstrapOrigin.Existing, null, " "));
    }
}
