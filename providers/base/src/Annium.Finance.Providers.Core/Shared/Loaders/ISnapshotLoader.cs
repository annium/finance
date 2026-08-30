using System;

namespace Annium.Finance.Providers.Core.Shared.Loaders;

/// <summary>
/// Repeatedly fetches a single snapshot of data on a timer, backing off to a slower interval once a fetch keeps
/// failing, until a fetch succeeds or the loader is stopped.
/// </summary>
/// <typeparam name="T">The type of data loaded.</typeparam>
public interface ISnapshotLoader<T> : IDisposable
{
    /// <summary>Raised with the loaded data every time a fetch succeeds.</summary>
    event Action<T> OnData;

    /// <summary>
    /// Starts fetching, on the fast interval, until a fetch succeeds (after which fetching stops) or the fast
    /// requests limit is reached (after which fetching continues on the slow interval).
    /// </summary>
    /// <param name="reportStatus">Whether to report a connecting status while the loader is active.</param>
    void Start(bool reportStatus);

    /// <summary>
    /// Stops fetching. Any fetch already in flight still completes, but its result is discarded.
    /// </summary>
    void Stop();
}
