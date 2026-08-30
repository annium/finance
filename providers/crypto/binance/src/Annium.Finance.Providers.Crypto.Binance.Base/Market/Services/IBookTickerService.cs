using System;
using System.Collections.Generic;
using Annium.Finance.Providers.Abstractions.Domain.Market;

namespace Annium.Finance.Providers.Crypto.Binance.Base.Market.Services;

/// <summary>Streams best bid/ask price updates for subscribed symbols from Binance's <c>bookTicker</c> WebSocket topics.</summary>
public interface IBookTickerService : IDisposable
{
    /// <summary>Raised for every book ticker update received for a subscribed symbol.</summary>
    event Action<InstrumentTicker> OnData;

    /// <summary>Subscribes to book ticker updates for the given symbols.</summary>
    /// <param name="symbols">The instrument symbols to subscribe to.</param>
    void Subscribe(IReadOnlyCollection<string> symbols);

    /// <summary>Unsubscribes from book ticker updates for the given symbols.</summary>
    /// <param name="symbols">The instrument symbols to unsubscribe from.</param>
    void Unsubscribe(IReadOnlyCollection<string> symbols);
}
