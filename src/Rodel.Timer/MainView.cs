using Rodel.Timer.Core.Bootstrap;

using Sprout;
using Sprout.Graphics;
using Sprout.Reconcile;

using static Sprout.Markup;

namespace Rodel.Timer;

public sealed partial class MainView : Control
{
    private readonly Window _window;

    public MainView(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);
        _window = window;
        Heading = "Rodel Timer";
        Status = "Preparing your timer data...";
    }

    public partial string Heading { get; private set; }

    public partial string Status { get; private set; }

    public void ShowReady(TimerBootstrapResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        string origin = result.Origin switch
        {
            TimerBootstrapOrigin.Migrated => "Legacy Work-Timer data migrated",
            TimerBootstrapOrigin.Created => "New timer data created",
            _ => "Timer data loaded",
        };
        string warning = result.Warnings.Count == 0
            ? string.Empty
            : $"\n{result.Warnings.Count} migration warning(s) require review.";

        Status =
            $"{origin}: {result.Workspace.Folders.Count} folder(s), "
            + $"{result.Workspace.Entries.Count} history item(s).{warning}\n\n"
            + "The Fluent product UI will be built on this verified foundation.";
    }

    public void ShowDataError(string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        Heading = "Rodel Timer could not load its data";
        Status = message;
    }

    private FoundationView Build()
    {
        var colors = _window.Theme.Colors;
        return new FoundationView(
            Heading,
            Status,
            colors.TextFillColorPrimary,
            colors.TextFillColorSecondary);
    }
}

[Composite]
public readonly record struct FoundationView(
    string Title,
    string Message,
    Color TitleColor,
    Color MessageColor)
{
    public Ui Body => Border(
        Stack(
            Text(
                Title,
                new TextStyle(
                    TextStyle.DefaultFontFamily,
                    28f,
                    TextStyle.NormalWeight,
                    TextAlignment.Center,
                    TextWrapping.Wrap),
                TitleColor),
            Text(
                Message,
                new TextStyle(
                    TextStyle.DefaultFontFamily,
                    15f,
                    TextStyle.NormalWeight,
                    TextAlignment.Center,
                    TextWrapping.Wrap),
                MessageColor))
        .Spacing(16f))
        .Padding(40f);
}
