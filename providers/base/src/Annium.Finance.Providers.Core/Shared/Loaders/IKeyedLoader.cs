using System;

namespace Annium.Finance.Providers.Core.Shared.Loaders;

/// <summary>
/// Runs a separate <see cref="ICompositeLoader{T}"/> per key, lazily created on first <see cref="Request"/>, each
/// carrying its own context that is threaded through and updated on every successful load for that key.
/// </summary>
/// <typeparam name="TKey">The type of key identifying each independent load.</typeparam>
/// <typeparam name="TContext">The type of per-key context passed to and updated by loads.</typeparam>
/// <typeparam name="TData">The type of data loaded.</typeparam>
public interface IKeyedLoader<TKey, TContext, TData> : IDisposable
    where TKey : notnull
{
    /// <summary>Raised with the key, its (pre-update) context, and the loaded data every time a load succeeds.</summary>
    event Action<TKey, TContext, TData> OnData;

    /// <summary>
    /// Requests a load for the given key, creating and starting a loader for it if this is the first request
    /// for that key.
    /// </summary>
    /// <param name="key">The key to request a load for.</param>
    void Request(TKey key);
}
