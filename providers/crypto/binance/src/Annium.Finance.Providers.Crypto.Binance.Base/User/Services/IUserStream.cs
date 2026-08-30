using System;

namespace Annium.Finance.Providers.Crypto.Binance.Base.User.Services;

/// <summary>Maintains the WebSocket connection to Binance's user data stream, delivering account, order and trade update messages.</summary>
public interface IUserStream : IDisposable
{
    /// <summary>Raised when the user data stream WebSocket connects.</summary>
    event Action OnConnected;

    /// <summary>Raised when the user data stream WebSocket disconnects.</summary>
    event Action OnDisconnected;

    /// <summary>Raised for every raw message received over the user data stream.</summary>
    event Action<ReadOnlyMemory<byte>> OnMessage;
}
