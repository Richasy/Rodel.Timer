using Rodel.Timer.Core.Folders;

using Sprout.Controls;

namespace Rodel.Timer.Presentation;

internal readonly record struct FolderIconOption(
    TimerFolderIcon Icon,
    FluentSymbol Symbol);

internal static class FolderIconCatalog
{
    public static IReadOnlyList<FolderIconOption> All { get; } =
    [
        new(TimerFolderIcon.Activity, FluentSymbol.BroadActivityFeed),
        new(TimerFolderIcon.Airplay, FluentSymbol.Cast),
        new(TimerFolderIcon.AlertTriangle, FluentSymbol.AlertUrgent),
        new(TimerFolderIcon.Anchor, FluentSymbol.Navigation),
        new(TimerFolderIcon.Aperture, FluentSymbol.CameraSparkles),
        new(TimerFolderIcon.Archive, FluentSymbol.Archive),
        new(TimerFolderIcon.Award, FluentSymbol.Trophy),
        new(TimerFolderIcon.BarChart, FluentSymbol.DataBarVertical),
        new(TimerFolderIcon.BarChart2, FluentSymbol.DataBarHorizontal),
        new(TimerFolderIcon.Battery, FluentSymbol.Battery10),
        new(TimerFolderIcon.Bluetooth, FluentSymbol.Bluetooth),
        new(TimerFolderIcon.Book, FluentSymbol.Book),
        new(TimerFolderIcon.Bookmark, FluentSymbol.Bookmark),
        new(TimerFolderIcon.Box, FluentSymbol.Box),
        new(TimerFolderIcon.Briefcase, FluentSymbol.Briefcase),
        new(TimerFolderIcon.Calendar, FluentSymbol.Calendar),
        new(TimerFolderIcon.Camera, FluentSymbol.Camera),
        new(TimerFolderIcon.Cast, FluentSymbol.Cast),
        new(TimerFolderIcon.Chrome, FluentSymbol.Globe),
        new(TimerFolderIcon.Clipboard, FluentSymbol.Clipboard),
        new(TimerFolderIcon.Clock, FluentSymbol.Clock),
        new(TimerFolderIcon.Cloud, FluentSymbol.Cloud),
        new(TimerFolderIcon.Code, FluentSymbol.Code),
        new(TimerFolderIcon.Coffee, FluentSymbol.DrinkCoffee),
        new(TimerFolderIcon.Cpu, FluentSymbol.DeveloperBoard),
        new(TimerFolderIcon.CreditCard, FluentSymbol.WalletCreditCard),
        new(TimerFolderIcon.Disc, FluentSymbol.Record),
        new(TimerFolderIcon.Edit, FluentSymbol.Edit),
        new(TimerFolderIcon.Eye, FluentSymbol.Eye),
        new(TimerFolderIcon.Facebook, FluentSymbol.PeopleCommunity),
        new(TimerFolderIcon.Feather, FluentSymbol.LeafOne),
        new(TimerFolderIcon.Figma, FluentSymbol.DesignIdeas),
        new(TimerFolderIcon.File, FluentSymbol.Document),
        new(TimerFolderIcon.Folder, FluentSymbol.Folder),
        new(TimerFolderIcon.Framer, FluentSymbol.Frame),
        new(TimerFolderIcon.Gift, FluentSymbol.Gift),
        new(TimerFolderIcon.Home, FluentSymbol.Home),
        new(TimerFolderIcon.Image, FluentSymbol.Image),
        new(TimerFolderIcon.Inbox, FluentSymbol.MailInbox),
        new(TimerFolderIcon.Info, FluentSymbol.Info),
    ];

    public static FluentSymbol Resolve(TimerFolderIcon icon)
    {
        foreach (FolderIconOption option in All)
        {
            if (option.Icon == icon)
            {
                return option.Symbol;
            }
        }

        return FluentSymbol.BroadActivityFeed;
    }
}
