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

    /// <summary>
    /// The app logo (the Work Timer clock) as an <see cref="ImageSource"/>, read once from the embedded PNG and cached.
    /// </summary>
    /// <returns>The shared logo source.</returns>
    /// <exception cref="InvalidOperationException">The embedded logo resource is missing (a build/packaging error).</exception>
    public static ImageSource AppLogo()
    {
        lock (Gate)
        {
            if (_appLogo is not null)
            {
                return _appLogo;
            }

            Assembly assembly = typeof(TimerAssets).Assembly;
            using Stream stream = assembly.GetManifestResourceStream(AppLogoResourceName)
                ?? throw new InvalidOperationException($"The embedded app-logo resource '{AppLogoResourceName}' is missing.");

            using var buffer = new MemoryStream();
            stream.CopyTo(buffer);
            _appLogo = ImageSource.FromBytes(buffer.ToArray());
            return _appLogo;
        }
    }
}
