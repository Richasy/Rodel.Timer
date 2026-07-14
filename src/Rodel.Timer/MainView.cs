using Rodel.Timer.Core.Bootstrap;

using Sprout;
using Sprout.Graphics;
using Sprout.Theming;

using static Sprout.Markup;

namespace Rodel.Timer;

/// <summary>
/// The application's placeholder root view, shown while the timer workspace bootstraps and afterward to
/// report its outcome. This is a foundation surface only: it renders <see cref="Heading"/>/<see cref="Status"/>
/// as plain text and will be replaced by the real Fluent product UI once the data foundation is verified.
/// </summary>
public sealed partial class MainView : Control
{
    /// <summary>Creates the view in its initial "preparing data" state.</summary>
    public MainView()
    {
        Heading = "Rodel Timer";
        Status = "Preparing your timer data...";
    }

    /// <summary>The view's current title text.</summary>
    public partial string Heading { get; private set; }

    /// <summary>The view's current status/body text.</summary>
    public partial string Status { get; private set; }

    /// <summary>Updates the view to report a successful bootstrap outcome, summarizing its origin and contents.</summary>
    /// <param name="result">The bootstrap result to summarize.</param>
    /// <exception cref="ArgumentNullException"><paramref name="result"/> is <see langword="null"/>.</exception>
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

    /// <summary>
    /// Updates the view to report that bootstrapping failed with a data error, leaving the original,
    /// untouched data on disk (the caller is expected to have already avoided any destructive write).
    /// </summary>
    /// <param name="message">The message to display, describing the failure.</param>
    /// <exception cref="ArgumentException"><paramref name="message"/> is null, empty, or whitespace-only.</exception>
    public void ShowDataError(string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        Heading = "Rodel Timer could not load its data";
        Status = message;
    }

    /// <summary>The inline placeholder body, colored from the ambient window theme.</summary>
    public Ui Body => Border(
        Stack(
            Text(
                Heading,
                new TextStyle(
                    TextStyle.DefaultFontFamily,
                    28f,
                    TextStyle.NormalWeight,
                    TextAlignment.Center,
                    TextWrapping.Wrap),
                Theme.Resolve().Colors.TextFillColorPrimary),
            Text(
                Status,
                new TextStyle(
                    TextStyle.DefaultFontFamily,
                    15f,
                    TextStyle.NormalWeight,
                    TextAlignment.Center,
                    TextWrapping.Wrap),
                Theme.Resolve().Colors.TextFillColorSecondary))
        .Spacing(16f))
        .Padding(40f);
}
