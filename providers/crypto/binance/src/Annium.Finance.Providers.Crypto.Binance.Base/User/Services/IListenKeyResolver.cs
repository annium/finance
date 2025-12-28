using System;

namespace Annium.Finance.Providers.Crypto.Binance.Base.User.Services;

public interface IListenKeyResolver : IAsyncDisposable
{
    event Action<string> OnListenKeyFetched;
    event Action OnListenKeyReset;
    void RequestNewListenKey();
}
