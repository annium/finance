using System;

namespace Annium.Finance.Providers.Crypto.Binance.Base.User.Services;

public interface IUserStream : IDisposable
{
    event Action OnConnected;
    event Action OnDisconnected;
    event Action<ReadOnlyMemory<byte>> OnMessage;
}
