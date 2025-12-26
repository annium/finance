using Annium.Finance.Providers.Abstractions.Domain.Market;

namespace Annium.Finance.Providers.Abstractions.Connectors.Market;

public interface IMarketConnectorInstanceFactory
{
    IMarketConnector Create(MarketSettings settings, AsyncDisposableBox disposable);
}
