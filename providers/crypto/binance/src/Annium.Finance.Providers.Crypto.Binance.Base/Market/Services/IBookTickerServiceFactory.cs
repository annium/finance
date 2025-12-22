using Annium.Serialization.Abstractions;

namespace Annium.Finance.Providers.Crypto.Binance.Base.Market.Services;

public interface IBookTickerServiceFactory
{
    IBookTickerService Create(MarketConfigBase config, SerializerKey serializerKey);
}
