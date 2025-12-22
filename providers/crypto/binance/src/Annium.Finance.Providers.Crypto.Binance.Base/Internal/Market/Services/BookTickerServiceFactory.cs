using System;
using Annium.Core.DependencyInjection;
using Annium.Finance.Providers.Core.Shared.Status;
using Annium.Finance.Providers.Crypto.Binance.Base.Market;
using Annium.Finance.Providers.Crypto.Binance.Base.Market.Services;
using Annium.Logging;
using Annium.Serialization.Abstractions;

namespace Annium.Finance.Providers.Crypto.Binance.Base.Internal.Market.Services;

internal class BookTickerServiceFactory(IServiceProvider sp) : IBookTickerServiceFactory
{
    public IBookTickerService Create(MarketConfigBase config, SerializerKey serializerKey)
    {
        var serializer = sp.ResolveSerializer<ReadOnlyMemory<byte>>(serializerKey.Key, serializerKey.MediaType);
        var statusReporter = sp.Resolve<IStatusReporter>();
        var logger = sp.Resolve<ILogger>();

        return new BookTickerService(config, serializer, statusReporter, logger);
    }
}
