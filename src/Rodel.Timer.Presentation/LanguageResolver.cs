using System.Globalization;

using Rodel.Timer.Core.Settings;

namespace Rodel.Timer.Presentation;

/// <summary>
/// Maps between the stored language preference (a supported concrete tag or the
/// <see cref="TimerPreferences.SystemLanguage"/> "follow system" sentinel) and the effective concrete language to
/// apply, and resolves an OS culture to the nearest supported app language. Shared by the startup path and the runtime
/// "follow system" path so the two cannot drift, and pure/deterministic so it is unit-testable without any OS
/// dependency.
/// </summary>
public static class LanguageResolver
{
    /// <summary>Whether a stored preference means "follow the OS preferred UI language".</summary>
    public static bool IsFollowSystem(string? language) =>
        string.Equals(language, TimerPreferences.SystemLanguage, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Resolves an OS culture to the nearest supported app language: Traditional Chinese for a <c>zh-Hant</c> chain,
    /// Simplified Chinese for a <c>zh-Hans</c> chain or any other Chinese, otherwise English.
    /// </summary>
    /// <param name="culture">The OS culture to resolve (e.g. <see cref="CultureInfo.CurrentUICulture"/>).</param>
    /// <returns>A supported concrete language tag.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="culture"/> is <see langword="null"/>.</exception>
    public static string ResolveSupported(CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(culture);

        for (CultureInfo current = culture; !string.IsNullOrEmpty(current.Name); current = current.Parent)
        {
            if (current.Name.Equals("zh-Hant", StringComparison.OrdinalIgnoreCase))
            {
                return TimerPreferences.TraditionalChineseLanguage;
            }

            if (current.Name.Equals("zh-Hans", StringComparison.OrdinalIgnoreCase))
            {
                return TimerPreferences.ChineseLanguage;
            }
        }

        return culture.TwoLetterISOLanguageName.Equals("zh", StringComparison.OrdinalIgnoreCase)
            ? TimerPreferences.ChineseLanguage
            : TimerPreferences.EnglishLanguage;
    }

    /// <summary>
    /// Resolves the effective concrete language to apply for a stored preference: the OS-resolved language when the
    /// preference is the follow-system sentinel, otherwise the preference itself (already a supported concrete tag).
    /// </summary>
    /// <param name="preferenceLanguage">The stored language preference.</param>
    /// <param name="osCulture">The OS culture to resolve against when following the system.</param>
    /// <returns>The concrete language tag to apply.</returns>
    public static string ResolveEffective(string preferenceLanguage, CultureInfo osCulture) =>
        IsFollowSystem(preferenceLanguage) ? ResolveSupported(osCulture) : preferenceLanguage;
}
