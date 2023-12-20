using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Annium.Finance.Providers.Abstractions.Domain.Models;
using Annium.Finance.Providers.Crypto.Binance.Base.Contracts.Shared.Domain;
using Annium.Finance.Providers.Shared.Connectors;
using Annium.Logging;
using Annium.Serialization.Abstractions;

namespace Annium.Finance.Providers.Crypto.Binance.Base.Services;

public sealed class BookTickerService : WebSocketService
{
    private readonly ISerializer<ReadOnlyMemory<byte>> _serializer;
    public event Action<InstrumentTicker> OnData = delegate { };

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

    public void Subscribe(IReadOnlyCollection<string> symbols)
    {
        SubscribeTopics(GetSymbolsTopics(symbols));
    }

    public void Unsubscribe(IReadOnlyCollection<string> symbols)
    {
        UnsubscribeTopics(GetSymbolsTopics(symbols));
    }

    protected override void HandleData(ReadOnlyMemory<byte> raw)
    {
        var data = _serializer.Deserialize<StreamData<InstrumentTicker>?>(raw);
        if (data is null)
        {
            this.Trace<string>("bypass: {data}", Encoding.UTF8.GetString(raw.ToArray()));
            return;
        }

        this.Trace("send: {data}", data.Data);
        OnData(data.Data);
    }

    private IEnumerable<string> GetSymbolsTopics(IEnumerable<string> symbols) =>
        symbols.Select(x => $"{x.ToLowerInvariant()}@bookTicker");
}
