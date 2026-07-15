using Rodel.Timer.Core.Bootstrap;
using Rodel.Timer.Core.Folders;
using Rodel.Timer.Core.Settings;
using Rodel.Timer.Core.Workspace;
using Rodel.Timer.Presentation;

namespace Rodel.Timer.Tests.Presentation;

/// <summary>
/// Unit coverage of <see cref="MigrationNotice.ShouldShow"/> — the pure decision that surfaces the legacy-migration
/// warning notice only when the workspace was recovered by migration <em>and</em> that recovery raised at least one
/// non-fatal warning. Every other bootstrap path (existing / created) or a warning-free migration shows nothing.
/// </summary>
[TestClass]
public sealed class MigrationNoticeTests
{
    private static readonly DateTimeOffset Origin = new(2024, 1, 1, 8, 0, 0, TimeSpan.Zero);

    private static TimerWorkspace Workspace()
    {
        var folder = new TimerFolder(new Guid("11111111-1111-1111-1111-111111111111"), "Work", Origin, TimerFolderIcon.Briefcase);
        return TimerWorkspace.Restore([folder], [], folder.Id, TimerPreferences.Default);
    }

    private static TimerBootstrapResult Result(TimerBootstrapOrigin origin, params string[] warnings) =>
        new(Workspace(), origin, warnings);

    [TestMethod]
    public void Migrated_WithWarnings_ShouldShow()
    {
        Assert.IsTrue(MigrationNotice.ShouldShow(Result(TimerBootstrapOrigin.Migrated, "Repaired a truncated entry.")));
    }

    [TestMethod]
    public void Migrated_WithNoWarnings_ShouldNotShow()
    {
        Assert.IsFalse(MigrationNotice.ShouldShow(Result(TimerBootstrapOrigin.Migrated)));
    }

    [TestMethod]
    public void Existing_WithWarnings_ShouldNotShow()
    {
        Assert.IsFalse(MigrationNotice.ShouldShow(Result(TimerBootstrapOrigin.Existing, "ignored")));
    }

    [TestMethod]
    public void Created_WithWarnings_ShouldNotShow()
    {
        Assert.IsFalse(MigrationNotice.ShouldShow(Result(TimerBootstrapOrigin.Created, "ignored")));
    }

    [TestMethod]
    public void Null_Throws()
    {
        _ = Assert.Throws<ArgumentNullException>(() => MigrationNotice.ShouldShow(null!));
    }
}
