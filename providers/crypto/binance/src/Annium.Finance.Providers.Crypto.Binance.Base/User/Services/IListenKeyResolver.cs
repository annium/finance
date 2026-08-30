using System;

namespace Annium.Finance.Providers.Crypto.Binance.Base.User.Services;

/// <summary>Keeps a Binance user data stream listen key alive, fetching a new one and confirming the current one on Binance's required schedule.</summary>
public interface IListenKeyResolver : IAsyncDisposable
{
    /// <summary>Raised when a listen key is fetched or confirmed for the first time since a reset.</summary>
    event Action<string> OnListenKeyFetched;

    /// <summary>Raised when the current listen key is invalidated and must be re-fetched.</summary>
    event Action OnListenKeyReset;

    /// <summary>Discards the current listen key, if any, and requests a new one immediately.</summary>
    void RequestNewListenKey();
}
