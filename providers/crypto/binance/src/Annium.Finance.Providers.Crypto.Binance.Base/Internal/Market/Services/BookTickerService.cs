using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Annium.Finance.Providers.Abstractions.Domain.Market;
using Annium.Finance.Providers.Core.Shared.Status;
using Annium.Finance.Providers.Crypto.Binance.Base.Market;
using Annium.Finance.Providers.Crypto.Binance.Base.Market.Services;
using Annium.Finance.Providers.Crypto.Binance.Base.Shared.Contracts.Domain;
using Annium.Logging;
using Annium.Serialization.Abstractions;

namespace Annium.Finance.Providers.Crypto.Binance.Base.Internal.Market.Services;

/// <summary>
/// Streams best bid/ask price updates from Binance's <c>&lt;symbol&gt;@bookTicker</c> WebSocket topics and raises them as <see cref="InstrumentTicker"/> events.
/// </summary>
internal sealed class BookTickerService : WebSocketService, IBookTickerService
{
    /// <summary>Raised for every book ticker update received for a subscribed symbol.</summary>
    public event Action<InstrumentTicker> OnData = delegate { };

    /// <summary>The serializer used to deserialize incoming book ticker WebSocket messages.</summary>
    private readonly ISerializer<ReadOnlyMemory<byte>> _serializer;

    /// <summary>Initializes a new instance of the <see cref="BookTickerService"/> class and opens the underlying market WebSocket connection.</summary>
    /// <param name="config">The market configuration providing the WebSocket API endpoint.</param>
    /// <param name="serializer">The serializer used to deserialize incoming book ticker messages.</param>
    /// <param name="statusReporter">The reporter used to publish connection status changes.</param>
    /// <param name="logger">The logger to trace connection and subscription activity with.</param>
    public BookTickerService(
        MarketConfigBase config,
        ISerializer<ReadOnlyMemory<byte>> serializer,
        IStatusReporter statusReporter,
        ILogger logger
    )
        : base(config, statusReporter, logger)
    {
        _serializer = serializer;
    }

    /// <summary>Subscribes to the <c>bookTicker</c> topic for the given symbols.</summary>
    /// <param name="symbols">The instrument symbols to subscribe to.</param>
    public void Subscribe(IReadOnlyCollection<string> symbols)
    {
        SubscribeTopics(GetSymbolsTopics(symbols));
    }

    /// <summary>Unsubscribes from the <c>bookTicker</c> topic for the given symbols.</summary>
    /// <param name="symbols">The instrument symbols to unsubscribe from.</param>
    public void Unsubscribe(IReadOnlyCollection<string> symbols)
    {
        UnsubscribeTopics(GetSymbolsTopics(symbols));
    }

    /// <summary>Deserializes an incoming book ticker WebSocket message and raises it through <see cref="OnData"/>.</summary>
    /// <param name="raw">The raw UTF-8 text payload received over the WebSocket.</param>
    protected override void HandleData(ReadOnlyMemory<byte> raw)
    {
        var data = _serializer.Deserialize<StreamData<InstrumentTicker>?>(raw);
        if (data is null)
        {
            this.Trace<string>("bypass: {data}", Encoding.UTF8.GetString(raw.ToArray()));
            return;
        }

        // this.Trace("send: {data}", data.Data);
        OnData(data.Data);
    }

    /// <summary>Builds the Binance <c>bookTicker</c> topic name for each given symbol.</summary>
    /// <param name="symbols">The instrument symbols to build topic names for.</param>
    /// <returns>The lowercase <c>&lt;symbol&gt;@bookTicker</c> topic names.</returns>
    private IEnumerable<string> GetSymbolsTopics(IEnumerable<string> symbols) =>
        symbols.Select(x => $"{x.ToLowerInvariant()}@bookTicker");
}
