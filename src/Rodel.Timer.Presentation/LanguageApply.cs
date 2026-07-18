using System.Globalization;

namespace Rodel.Timer.Presentation;

/// <summary>
/// The three side effects a language switch must apply to the shell, in one place, so the persisted-load path and the
/// runtime settings-switch path cannot drift apart. The root view implements this; both call sites route through
/// <see cref="LanguageApply.Apply"/> so neither can forget a step.
/// </summary>
/// <remarks>
/// The load-order regression this seam guards against: on first load the reactive shell renders its rows the instant the
/// store flips to success — with the OS-default localizer language and the pre-load theme stamp. The persisted-language
/// apply then runs. If it only switched the localizer (and re-titled the window) but did <b>not</b> bump the theme
/// stamp, the already-mounted, same-key rows would produce equal row data (same folder, same selection, same stamp), so
/// the same-key rebind would no-op and the rows' accessibility phrases would stay frozen in the OS startup language.
/// Bumping the stamp is therefore not optional on either path — it is what re-values the mounted rows.
/// </remarks>
public interface ILanguageApplyTarget
{
    /// <summary>Switches the localizer's current language so every localized read re-resolves.</summary>
    /// <param name="language">The BCP-47 language tag to switch to.</param>
    void SetCurrentLanguage(string language);

    /// <summary>Re-titles the host window from the now-current language.</summary>
    void SyncWindowTitle();

    /// <summary>Bumps the theme/language revision so the already-mounted, same-key shell re-renders and re-localizes.</summary>
    void BumpThemeRevision();
}

/// <summary>
/// Applies a language switch's side effects to an <see cref="ILanguageApplyTarget"/> in one fixed order — the single
/// seam both the persisted-load and the runtime-switch paths share, so the theme-revision bump can never be dropped from
/// one of them again.
/// </summary>
public static class LanguageApply
{
    /// <summary>
    /// Applies the language everywhere: switches the localizer, re-titles the window, then bumps the theme revision (so
    /// the already-mounted shell re-renders in the new language). All three run unconditionally and in this order.
    /// </summary>
    /// <param name="target">The shell to apply the language to.</param>
    /// <param name="language">The BCP-47 language tag to switch to.</param>
    /// <exception cref="ArgumentNullException"><paramref name="target"/> is <see langword="null"/>.</exception>
    public static void Apply(ILanguageApplyTarget target, string language)
    {
        ArgumentNullException.ThrowIfNull(target);

        target.SetCurrentLanguage(language);
        target.SyncWindowTitle();
        target.BumpThemeRevision();
    }

    /// <summary>
    /// Pins the ambient thread culture (current + default-thread, both format and UI) to a language tag, so
    /// <see cref="CultureInfo.CurrentCulture"/>-based date/number formatting (history timestamps, footer minute counts)
    /// follows the app language instead of staying on the OS culture. A no-op for an unrecognized tag. Shared by the
    /// runtime language switch (via the shell's <c>SetCurrentLanguage</c>) and the startup language application.
    /// </summary>
    /// <param name="language">The BCP-47 language tag to pin.</param>
    public static void PinThreadCulture(string language)
    {
        CultureInfo culture;
        try
        {
            culture = CultureInfo.GetCultureInfo(language);
        }
        catch (CultureNotFoundException)
        {
            return;
        }

        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
    }
}
