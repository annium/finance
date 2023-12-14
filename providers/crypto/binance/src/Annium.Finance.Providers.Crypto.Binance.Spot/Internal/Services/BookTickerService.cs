using System;
using System.Net.Mime;
using Annium.Core.DependencyInjection;
using Annium.Finance.Providers.Crypto.Binance.Base.Services;
using Annium.Finance.Providers.Shared.Connectors;
using Annium.Logging;
using Annium.Serialization.Abstractions;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Internal.Services;

internal sealed class BookTickerService : BookTickerServiceBase
{
    public BookTickerService(
        MarketConfig config,
        IIndex<SerializerKey, ISerializer<ReadOnlyMemory<byte>>> serializers,
        IStatusReporter statusReporter,
        ILogger logger
    )
        : base(
            config,
            serializers[SerializerKey.Create(Constants.InstrumentTickerKey, MediaTypeNames.Application.Json)],
            statusReporter,
            logger
        ) { }
}
