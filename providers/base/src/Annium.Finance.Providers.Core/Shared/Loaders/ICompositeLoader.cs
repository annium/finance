using System;

namespace Annium.Finance.Providers.Core.Shared.Loaders;

/// <summary>
/// Wraps an <see cref="ISnapshotLoader{T}"/> with two extra ways to trigger reloads while active: a fixed
/// interval timer, and a debounced <see cref="Request"/> call that coalesces bursts of requests into a single
/// reload. A failed reload is retried by the underlying snapshot loader; it does not stop the composite loader.
/// </summary>
/// <typeparam name="T">The type of data loaded.</typeparam>
public interface ICompositeLoader<T> : IDisposable
{
    /// <summary>Raised with the loaded data every time a reload succeeds.</summary>
    event Action<T> OnData;

    /// <summary>
    /// Starts the underlying snapshot loader and arms the interval and debounce timers.
    /// </summary>
    /// <param name="reportStatus">Whether to report a connecting status for the initial load.</param>
    void Start(bool reportStatus);

    /// <summary>
    /// Stops the underlying snapshot loader and disarms the interval and debounce timers.
    /// </summary>
    void Stop();

    /// <summary>
    /// Requests a reload via the debounce timer. Multiple calls within the debounce period collapse into a
    /// single reload. Has no effect unless the loader is active.
    /// </summary>
    void Request();
}
