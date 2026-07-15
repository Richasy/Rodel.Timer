namespace Rodel.Timer.Presentation;

/// <summary>
/// The pure policy that decides whether an asynchronous data-load's continuation may still apply its result to the UI.
/// A load is kicked off after the window opens; if the window is closed while the load is in flight, the store is
/// disposed and its continuation must <b>not</b> touch it or push a "loaded" state onto a closing window. The load task
/// also completes normally on an internal error/cancel (the failure is carried by the reactive bootstrap state, not an
/// exception), so a successful <c>await</c> does not by itself mean there is a value to apply.
/// </summary>
public static class AppLoadPolicy
{
    /// <summary>Whether a completed data-load may apply its result (the store is still alive and the bootstrap succeeded).</summary>
    /// <param name="storeDisposed">Whether the store was disposed (the window closed) while the load was in flight.</param>
    /// <param name="bootstrapSucceeded">Whether the bootstrap completed with a value (a successful load).</param>
    /// <returns><see langword="true"/> only when the store is alive and the bootstrap produced a value.</returns>
    public static bool ShouldApplyLoadedData(bool storeDisposed, bool bootstrapSucceeded) =>
        !storeDisposed && bootstrapSucceeded;
}
