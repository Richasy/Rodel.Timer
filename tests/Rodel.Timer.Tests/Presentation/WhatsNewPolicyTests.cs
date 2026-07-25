using Rodel.Timer.Core.Bootstrap;
using Rodel.Timer.Presentation;

namespace Rodel.Timer.Tests.Presentation;

/// <summary>
/// Unit coverage of <see cref="WhatsNewPolicy.Decide"/> — the pure decision behind the upgrade notice. It must never
/// appear on a brand-new install (<see cref="WhatsNewAction.RecordOnly"/>), never appear twice for the same build
/// (<see cref="WhatsNewAction.None"/> once the current version is recorded), and never replay notes the user has
/// already read when a later release ships without new ones.
/// </summary>
[TestClass]
public sealed class WhatsNewPolicyTests
{
    private const string Current = "2.1.0";
    private const string Notice = "2.1.0";

    [TestMethod]
    public void Created_WithNoLastSeenVersion_RecordsOnly()
    {
        Assert.AreEqual(WhatsNewAction.RecordOnly, WhatsNewPolicy.Decide(TimerBootstrapOrigin.Created, null, Current, Notice));
    }

    [TestMethod]
    public void Created_WithAnOlderLastSeenVersion_RecordsOnly()
    {
        Assert.AreEqual(WhatsNewAction.RecordOnly, WhatsNewPolicy.Decide(TimerBootstrapOrigin.Created, "1.0.0", Current, Notice));
    }

    [TestMethod]
    public void Created_WhenAlreadyAtCurrentVersion_DoesNothing()
    {
        Assert.AreEqual(WhatsNewAction.None, WhatsNewPolicy.Decide(TimerBootstrapOrigin.Created, Current, Current, Notice));
    }

    [TestMethod]
    public void Existing_WithNoLastSeenVersion_Shows()
    {
        Assert.AreEqual(WhatsNewAction.Show, WhatsNewPolicy.Decide(TimerBootstrapOrigin.Existing, null, Current, Notice));
    }

    [TestMethod]
    public void Existing_WithAnOlderLastSeenVersion_Shows()
    {
        Assert.AreEqual(WhatsNewAction.Show, WhatsNewPolicy.Decide(TimerBootstrapOrigin.Existing, "2.0.0", Current, Notice));
    }

    [TestMethod]
    public void Existing_WhenAlreadyAtCurrentVersion_DoesNothing()
    {
        Assert.AreEqual(WhatsNewAction.None, WhatsNewPolicy.Decide(TimerBootstrapOrigin.Existing, Current, Current, Notice));
    }

    [TestMethod]
    public void Migrated_WithAnOlderLastSeenVersion_Shows()
    {
        Assert.AreEqual(WhatsNewAction.Show, WhatsNewPolicy.Decide(TimerBootstrapOrigin.Migrated, "1.0.0", Current, Notice));
    }

    [TestMethod]
    public void Migrated_WithNoLastSeenVersion_Shows()
    {
        Assert.AreEqual(WhatsNewAction.Show, WhatsNewPolicy.Decide(TimerBootstrapOrigin.Migrated, null, Current, Notice));
    }

    [TestMethod]
    public void Migrated_WhenAlreadyAtCurrentVersion_DoesNothing()
    {
        Assert.AreEqual(WhatsNewAction.None, WhatsNewPolicy.Decide(TimerBootstrapOrigin.Migrated, Current, Current, Notice));
    }

    [TestMethod]
    public void Existing_UpgradeWithNoNewNotes_RecordsSilently()
    {
        // The 2.1.0 notes have been read; 2.1.1 is a bug-fix release shipping the same text. It must upgrade quietly.
        Assert.AreEqual(
            WhatsNewAction.RecordOnly,
            WhatsNewPolicy.Decide(TimerBootstrapOrigin.Existing, "2.1.0", "2.1.1", "2.1.0"));
    }

    [TestMethod]
    public void Existing_UpgradeFromNewerThanTheNotice_RecordsSilently()
    {
        Assert.AreEqual(
            WhatsNewAction.RecordOnly,
            WhatsNewPolicy.Decide(TimerBootstrapOrigin.Existing, "2.2.0", "2.3.0", "2.1.0"));
    }

    [TestMethod]
    public void Existing_UpgradeThatShipsFreshNotes_Shows()
    {
        Assert.AreEqual(
            WhatsNewAction.Show,
            WhatsNewPolicy.Decide(TimerBootstrapOrigin.Existing, "2.1.1", "2.2.0", "2.2.0"));
    }

    [TestMethod]
    public void Existing_SkippedRelease_StillShowsTheLatestNotes()
    {
        // The user jumped 2.0.0 → 2.3.0 and never saw the 2.2.0 notes; they are still unread, so they are shown.
        Assert.AreEqual(
            WhatsNewAction.Show,
            WhatsNewPolicy.Decide(TimerBootstrapOrigin.Existing, "2.0.0", "2.3.0", "2.2.0"));
    }

    [TestMethod]
    public void Existing_UnparsableLastSeenVersion_ShowsRatherThanSwallowing()
    {
        Assert.AreEqual(
            WhatsNewAction.Show,
            WhatsNewPolicy.Decide(TimerBootstrapOrigin.Existing, "not-a-version", Current, Notice));
    }

    [TestMethod]
    public void Decide_WithNoCurrentVersion_Throws()
    {
        _ = Assert.Throws<ArgumentException>(
            () => WhatsNewPolicy.Decide(TimerBootstrapOrigin.Existing, null, " ", Notice));
    }

    [TestMethod]
    public void Decide_WithNoNoticeVersion_Throws()
    {
        _ = Assert.Throws<ArgumentException>(
            () => WhatsNewPolicy.Decide(TimerBootstrapOrigin.Existing, null, Current, " "));
    }
}
