using Annium.Finance.Providers.Abstractions.Domain.Market;

namespace Annium.Finance.Providers.Abstractions.Connectors.Market;

public interface IMarketConnectorFactory
{
    IMarketConnector Create(MarketSettings settings);
}
