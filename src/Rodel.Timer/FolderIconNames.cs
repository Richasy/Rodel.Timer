using Rodel.Timer.Core.Folders;

namespace Rodel.Timer;

/// <summary>
/// Resolves a <see cref="TimerFolderIcon"/> to its localized, human-readable display name for accessibility (the icon
/// picker cell's UI Automation <c>Name</c>). This lives in the app layer — not in <c>Rodel.Timer.Presentation</c> — so
/// the presentation library stays free of the generated <see cref="Resources"/> localization surface; the mapping is an
/// exhaustive switch over the catalog, with no silent enum-name fallback (an unmapped value throws so a newly added
/// icon cannot ship an English technical enum name to a screen reader).
/// </summary>
internal static class FolderIconNames
{
    /// <summary>Resolves a folder icon to its localized display name.</summary>
    /// <param name="icon">The folder icon to name.</param>
    /// <returns>The localized, human-readable name for <paramref name="icon"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="icon"/> is not a mapped catalog icon.</exception>
    public static string Resolve(TimerFolderIcon icon) => icon switch
    {
        TimerFolderIcon.Activity => Resources.FolderIconActivity,
        TimerFolderIcon.Airplay => Resources.FolderIconAirplay,
        TimerFolderIcon.AlertTriangle => Resources.FolderIconAlertTriangle,
        TimerFolderIcon.Anchor => Resources.FolderIconAnchor,
        TimerFolderIcon.Aperture => Resources.FolderIconAperture,
        TimerFolderIcon.Archive => Resources.FolderIconArchive,
        TimerFolderIcon.Award => Resources.FolderIconAward,
        TimerFolderIcon.BarChart => Resources.FolderIconBarChart,
        TimerFolderIcon.BarChart2 => Resources.FolderIconBarChart2,
        TimerFolderIcon.Battery => Resources.FolderIconBattery,
        TimerFolderIcon.Bluetooth => Resources.FolderIconBluetooth,
        TimerFolderIcon.Book => Resources.FolderIconBook,
        TimerFolderIcon.Bookmark => Resources.FolderIconBookmark,
        TimerFolderIcon.Box => Resources.FolderIconBox,
        TimerFolderIcon.Briefcase => Resources.FolderIconBriefcase,
        TimerFolderIcon.Calendar => Resources.FolderIconCalendar,
        TimerFolderIcon.Camera => Resources.FolderIconCamera,
        TimerFolderIcon.Cast => Resources.FolderIconCast,
        TimerFolderIcon.Chrome => Resources.FolderIconChrome,
        TimerFolderIcon.Clipboard => Resources.FolderIconClipboard,
        TimerFolderIcon.Clock => Resources.FolderIconClock,
        TimerFolderIcon.Cloud => Resources.FolderIconCloud,
        TimerFolderIcon.Code => Resources.FolderIconCode,
        TimerFolderIcon.Coffee => Resources.FolderIconCoffee,
        TimerFolderIcon.Cpu => Resources.FolderIconCpu,
        TimerFolderIcon.CreditCard => Resources.FolderIconCreditCard,
        TimerFolderIcon.Disc => Resources.FolderIconDisc,
        TimerFolderIcon.Edit => Resources.FolderIconEdit,
        TimerFolderIcon.Eye => Resources.FolderIconEye,
        TimerFolderIcon.Facebook => Resources.FolderIconFacebook,
        TimerFolderIcon.Feather => Resources.FolderIconFeather,
        TimerFolderIcon.Figma => Resources.FolderIconFigma,
        TimerFolderIcon.File => Resources.FolderIconFile,
        TimerFolderIcon.Folder => Resources.FolderIconFolder,
        TimerFolderIcon.Framer => Resources.FolderIconFramer,
        TimerFolderIcon.Gift => Resources.FolderIconGift,
        TimerFolderIcon.Home => Resources.FolderIconHome,
        TimerFolderIcon.Image => Resources.FolderIconImage,
        TimerFolderIcon.Inbox => Resources.FolderIconInbox,
        TimerFolderIcon.Info => Resources.FolderIconInfo,
        _ => throw new ArgumentOutOfRangeException(nameof(icon), icon, "No localized name is mapped for this folder icon."),
    };
}
