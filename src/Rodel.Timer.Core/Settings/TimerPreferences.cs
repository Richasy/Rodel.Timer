namespace Rodel.Timer.Core.Settings;

public sealed record TimerPreferences
{
    public const int DefaultDailyTargetMinutes = 360;
    public const int MaximumDailyTargetMinutes = 1440;
    public const string DefaultFontFamily = "Microsoft YaHei UI";
    public const string EnglishLanguage = "en-US";
    public const string ChineseLanguage = "zh-CN";

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

    public static TimerPreferences Default { get; } = new(
        TimerThemeMode.System,
        EnglishLanguage,
        DefaultFontFamily,
        DefaultDailyTargetMinutes);

    public TimerThemeMode Theme { get; }

    public string Language { get; }

    public string FontFamily { get; }

    public int DailyTargetMinutes { get; }

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
