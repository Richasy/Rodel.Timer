namespace Rodel.Timer.Core.Folders;

/// <summary>
/// A glyph shown to represent a <see cref="TimerFolder"/>, named after its corresponding Feather icon
/// (https://feathericons.com/). <see cref="Migration.LegacyIconMap"/> maps legacy Work-Timer icon codepoints
/// onto this set, falling back to <see cref="Activity"/> for any unrecognized codepoint.
/// </summary>
public enum TimerFolderIcon
{
    /// <summary>The Feather "activity" icon; the default fallback for an unrecognized legacy icon.</summary>
    Activity,

    /// <summary>The Feather "airplay" icon.</summary>
    Airplay,

    /// <summary>The Feather "alert-triangle" icon.</summary>
    AlertTriangle,

    /// <summary>The Feather "anchor" icon.</summary>
    Anchor,

    /// <summary>The Feather "aperture" icon.</summary>
    Aperture,

    /// <summary>The Feather "archive" icon.</summary>
    Archive,

    /// <summary>The Feather "award" icon.</summary>
    Award,

    /// <summary>The Feather "bar-chart" icon.</summary>
    BarChart,

    /// <summary>The Feather "bar-chart-2" icon.</summary>
    BarChart2,

    /// <summary>The Feather "battery" icon.</summary>
    Battery,

    /// <summary>The Feather "bluetooth" icon.</summary>
    Bluetooth,

    /// <summary>The Feather "book" icon.</summary>
    Book,

    /// <summary>The Feather "bookmark" icon.</summary>
    Bookmark,

    /// <summary>The Feather "box" icon.</summary>
    Box,

    /// <summary>The Feather "briefcase" icon.</summary>
    Briefcase,

    /// <summary>The Feather "calendar" icon.</summary>
    Calendar,

    /// <summary>The Feather "camera" icon.</summary>
    Camera,

    /// <summary>The Feather "cast" icon.</summary>
    Cast,

    /// <summary>The Feather "chrome" icon.</summary>
    Chrome,

    /// <summary>The Feather "clipboard" icon.</summary>
    Clipboard,

    /// <summary>The Feather "clock" icon.</summary>
    Clock,

    /// <summary>The Feather "cloud" icon.</summary>
    Cloud,

    /// <summary>The Feather "code" icon.</summary>
    Code,

    /// <summary>The Feather "coffee" icon.</summary>
    Coffee,

    /// <summary>The Feather "cpu" icon.</summary>
    Cpu,

    /// <summary>The Feather "credit-card" icon.</summary>
    CreditCard,

    /// <summary>The Feather "disc" icon.</summary>
    Disc,

    /// <summary>The Feather "edit" icon.</summary>
    Edit,

    /// <summary>The Feather "eye" icon.</summary>
    Eye,

    /// <summary>The Feather "facebook" icon.</summary>
    Facebook,

    /// <summary>The Feather "feather" icon.</summary>
    Feather,

    /// <summary>The Feather "figma" icon.</summary>
    Figma,

    /// <summary>The Feather "file" icon.</summary>
    File,

    /// <summary>The Feather "folder" icon.</summary>
    Folder,

    /// <summary>The Feather "framer" icon.</summary>
    Framer,

    /// <summary>The Feather "gift" icon.</summary>
    Gift,

    /// <summary>The Feather "home" icon.</summary>
    Home,

    /// <summary>The Feather "image" icon.</summary>
    Image,

    /// <summary>The Feather "inbox" icon.</summary>
    Inbox,

    /// <summary>The Feather "info" icon.</summary>
    Info,
}
