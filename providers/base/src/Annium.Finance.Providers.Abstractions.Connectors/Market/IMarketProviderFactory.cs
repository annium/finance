using Annium.Finance.Providers.Abstractions.Domain.Market;

namespace Annium.Finance.Providers.Abstractions.Connectors.Market;

public interface IMarketProviderFactory
{
    IMarketProvider Create(MarketSettings settings);
}
