namespace Rodel.Timer.Core.Settings;

/// <summary>
/// The immutable set of user-configurable preferences attached to a <see cref="Workspace.TimerWorkspace"/>:
/// theme, language, font, and the daily time target used to track progress toward a day's goal.
/// </summary>
public sealed record TimerPreferences
{
    /// <summary>The daily time target, in minutes, applied when no explicit value is supplied.</summary>
    public const int DefaultDailyTargetMinutes = 360;

    /// <summary>The largest daily time target allowed, in minutes (24 hours).</summary>
    public const int MaximumDailyTargetMinutes = 1440;

    /// <summary>The font family applied when no explicit value is supplied.</summary>
    public const string DefaultFontFamily = "Microsoft YaHei UI";

    /// <summary>The supported English language tag.</summary>
    public const string EnglishLanguage = "en-US";

    /// <summary>The supported Chinese language tag.</summary>
    public const string ChineseLanguage = "zh-CN";

    /// <summary>Creates a preferences record, normalizing <paramref name="language"/> and <paramref name="fontFamily"/>.</summary>
    /// <param name="theme">The preferred theme.</param>
    /// <param name="language">The preferred language. Must be <see cref="EnglishLanguage"/> or <see cref="ChineseLanguage"/> (case-insensitive).</param>
    /// <param name="fontFamily">The preferred font family. Must not be null, empty, or whitespace-only.</param>
    /// <param name="dailyTargetMinutes">The daily time target, in minutes. Must be between 1 and <see cref="MaximumDailyTargetMinutes"/>.</param>
    /// <param name="lastSeenVersion">The application version that last wrote these preferences, or <see langword="null"/> if unknown.</param>
    /// <exception cref="ArgumentException"><paramref name="language"/> is not a supported language, or <paramref name="fontFamily"/> is null, empty, or whitespace-only.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="dailyTargetMinutes"/> is outside the valid range.</exception>
    public TimerPreferences(
        TimerThemeMode theme,
        string language,
        string fontFamily,
        int dailyTargetMinutes,
        string? lastSeenVersion = null)
    {
        Theme = theme;
        Language = NormalizeLanguage(language);
        FontFamily = NormalizeFontFamily(fontFamily);

        if (dailyTargetMinutes is < 1 or > MaximumDailyTargetMinutes)
        {
            throw new ArgumentOutOfRangeException(
                nameof(dailyTargetMinutes),
                dailyTargetMinutes,
                $"The daily target must be between 1 and {MaximumDailyTargetMinutes} minutes.");
        }

        DailyTargetMinutes = dailyTargetMinutes;
        LastSeenVersion = string.IsNullOrWhiteSpace(lastSeenVersion) ? null : lastSeenVersion.Trim();
    }

    /// <summary>The default preferences: system theme, English language, the default font, and the default daily target.</summary>
    public static TimerPreferences Default { get; } = new(
        TimerThemeMode.System,
        EnglishLanguage,
        DefaultFontFamily,
        DefaultDailyTargetMinutes);

    /// <summary>The preferred theme.</summary>
    public TimerThemeMode Theme { get; }

    /// <summary>The preferred language: <see cref="EnglishLanguage"/> or <see cref="ChineseLanguage"/>.</summary>
    public string Language { get; }

    /// <summary>The preferred, trimmed, non-empty font family.</summary>
    public string FontFamily { get; }

    /// <summary>The daily time target, in minutes; between 1 and <see cref="MaximumDailyTargetMinutes"/>.</summary>
    public int DailyTargetMinutes { get; }

    /// <summary>The application version that last wrote these preferences, or <see langword="null"/> if unknown.</summary>
    public string? LastSeenVersion { get; }

    private static string NormalizeLanguage(string language)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(language);

        if (string.Equals(language, EnglishLanguage, StringComparison.OrdinalIgnoreCase))
        {
            return EnglishLanguage;
        }

        if (string.Equals(language, ChineseLanguage, StringComparison.OrdinalIgnoreCase))
        {
            return ChineseLanguage;
        }

        throw new ArgumentException($"Unsupported language '{language}'.", nameof(language));
    }

    private static string NormalizeFontFamily(string fontFamily)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fontFamily);
        return fontFamily.Trim();
    }
}
