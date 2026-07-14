using Rodel.Timer.Core.Folders;

namespace Rodel.Timer.Core.Migration;

internal static class LegacyIconMap
{
    private static readonly Dictionary<int, TimerFolderIcon> Icons =
        new Dictionary<int, TimerFolderIcon>
        {
            [0xE900] = TimerFolderIcon.Activity,
            [0xE901] = TimerFolderIcon.Airplay,
            [0xE904] = TimerFolderIcon.AlertTriangle,
            [0xE909] = TimerFolderIcon.Anchor,
            [0xE90A] = TimerFolderIcon.Aperture,
            [0xE90B] = TimerFolderIcon.Archive,
            [0xE919] = TimerFolderIcon.Award,
            [0xE91A] = TimerFolderIcon.BarChart,
            [0xE91B] = TimerFolderIcon.BarChart2,
            [0xE91C] = TimerFolderIcon.Battery,
            [0xE920] = TimerFolderIcon.Bluetooth,
            [0xE922] = TimerFolderIcon.Book,
            [0xE924] = TimerFolderIcon.Bookmark,
            [0xE925] = TimerFolderIcon.Box,
            [0xE926] = TimerFolderIcon.Briefcase,
            [0xE927] = TimerFolderIcon.Calendar,
            [0xE928] = TimerFolderIcon.Camera,
            [0xE92A] = TimerFolderIcon.Cast,
            [0xE936] = TimerFolderIcon.Chrome,
            [0xE938] = TimerFolderIcon.Clipboard,
            [0xE939] = TimerFolderIcon.Clock,
            [0xE93A] = TimerFolderIcon.Cloud,
            [0xE940] = TimerFolderIcon.Code,
            [0xE943] = TimerFolderIcon.Coffee,
            [0xE950] = TimerFolderIcon.Cpu,
            [0xE951] = TimerFolderIcon.CreditCard,
            [0xE956] = TimerFolderIcon.Disc,
            [0xE95B] = TimerFolderIcon.Edit,
            [0xE95F] = TimerFolderIcon.Eye,
            [0xE961] = TimerFolderIcon.Facebook,
            [0xE963] = TimerFolderIcon.Feather,
            [0xE964] = TimerFolderIcon.Figma,
            [0xE965] = TimerFolderIcon.File,
            [0xE96C] = TimerFolderIcon.Folder,
            [0xE96F] = TimerFolderIcon.Framer,
            [0xE971] = TimerFolderIcon.Gift,
            [0xE980] = TimerFolderIcon.Home,
            [0xE981] = TimerFolderIcon.Image,
            [0xE982] = TimerFolderIcon.Inbox,
            [0xE983] = TimerFolderIcon.Info,
        };

    public static TimerFolderIcon Resolve(int legacyValue, List<string> warnings)
    {
        if (Icons.TryGetValue(legacyValue, out TimerFolderIcon icon))
        {
            return icon;
        }

        warnings.Add(
            $"Legacy folder icon U+{legacyValue:X4} is unknown and was replaced with Activity.");
        return TimerFolderIcon.Activity;
    }
}
