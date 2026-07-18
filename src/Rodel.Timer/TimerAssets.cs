using System.Reflection;
using System.Threading;

using Sprout.Graphics;

namespace Rodel.Timer;

/// <summary>
/// Loads the app's embedded image assets. The app logo is compiled into the assembly as an <c>EmbeddedResource</c> so
/// it resolves identically whether the app runs packaged (MSIX) or as the loose published exe — the MSIX <c>Assets</c>
/// are not copied to the loose publish output, so a file-relative <see cref="ImageSource.FromAppAsset"/> would fail there.
/// </summary>
internal static class TimerAssets
{
    private const string AppLogoResourceName = "Rodel.Timer.AppLogo.png";

    private static readonly Lock Gate = new();
    private static ImageSource? _appLogo;
    private static ImageHandle? _appLogoHandle;

    /// <summary>
    /// The app logo (the Work Timer clock) as an <see cref="ImageSource"/>, read once from the embedded PNG and cached.
    /// </summary>
    /// <returns>The shared logo source.</returns>
    /// <exception cref="InvalidOperationException">The embedded logo resource is missing (a build/packaging error).</exception>
    public static ImageSource AppLogo()
    {
        lock (Gate)
        {
            return _appLogo ??= LoadLogoSource();
        }
    }

    /// <summary>
    /// The app logo as a reference-stable <see cref="ImageHandle"/>, created once and cached. Prefer this over
    /// <see cref="AppLogo"/> wherever the logo is drawn from a view that rebuilds (the title-bar brand), because an
    /// <c>ImageView</c> / <c>ImageIcon</c> must hold a stable handle across rebuilds — a fresh handle per render
    /// re-decodes the PNG and flashes the async placeholder.
    /// </summary>
    /// <returns>The shared logo handle.</returns>
    /// <exception cref="InvalidOperationException">The embedded logo resource is missing (a build/packaging error).</exception>
    public static ImageHandle AppLogoHandle()
    {
        lock (Gate)
        {
            return _appLogoHandle ??= new ImageHandle(_appLogo ??= LoadLogoSource());
        }
    }

    private static ImageSource LoadLogoSource()
    {
        Assembly assembly = typeof(TimerAssets).Assembly;
        using Stream stream = assembly.GetManifestResourceStream(AppLogoResourceName)
            ?? throw new InvalidOperationException($"The embedded app-logo resource '{AppLogoResourceName}' is missing.");

        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return ImageSource.FromBytes(buffer.ToArray());
    }
}
