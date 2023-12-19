using System;
using System.Net.Mime;
using Annium.Core.DependencyInjection;
using Annium.Finance.Providers.Crypto.Binance.Base.Services;
using Annium.Finance.Providers.Shared.Connectors;
using Annium.Logging;
using Annium.Serialization.Abstractions;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Services;

internal sealed class BookTickerService : BookTickerServiceBase
{
    public BookTickerService(IServiceProvider sp, MarketConfig config, IStatusReporter statusReporter, ILogger logger)
        : base(
            config,
            sp.ResolveKeyed<ISerializer<ReadOnlyMemory<byte>>>(
                SerializerKey.Create(Constants.InstrumentTickerKey, MediaTypeNames.Application.Json)
            ),
            statusReporter,
            logger
        ) { }
}
