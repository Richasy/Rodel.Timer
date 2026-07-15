using Rodel.Timer.Presentation;

namespace Rodel.Timer.Tests.Presentation;

/// <summary>
/// Unit coverage of <see cref="LanguageApply"/> — the single seam the persisted-load and runtime-switch paths share.
/// The crux is that <see cref="ILanguageApplyTarget.BumpThemeRevision"/> is <b>always</b> invoked (the regression that
/// froze the already-mounted, same-key rows in the OS startup language was exactly a missing bump on the load path), and
/// that the three effects run in the fixed order localizer → title → revision.
/// </summary>
[TestClass]
public sealed class LanguageApplyTests
{
    private sealed class RecordingTarget : ILanguageApplyTarget
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

    [TestMethod]
    public void Apply_InvokesAllThreeEffectsInOrder()
    {
        var target = new RecordingTarget();

        LanguageApply.Apply(target, "zh-CN");

        string[] expected = ["language", "title", "revision"];
        CollectionAssert.AreEqual(expected, target.Calls);
        Assert.AreEqual("zh-CN", target.Language);
    }

    [TestMethod]
    public void Apply_AlwaysBumpsTheThemeRevision()
    {
        // The load-path regression guard: whatever else happens, the revision bump must fire so the mounted rows
        // re-localize. Asserting its presence directly pins the fix.
        var target = new RecordingTarget();

        LanguageApply.Apply(target, "en-US");

        Assert.Contains("revision", target.Calls);
    }

    [TestMethod]
    public void Apply_WithNullTarget_Throws() =>
        Assert.ThrowsExactly<ArgumentNullException>(static () => LanguageApply.Apply(null!, "en-US"));
}
