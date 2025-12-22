using Annium.Finance.Providers.Abstractions.Domain.Shared;

namespace Annium.Finance.Providers.Abstractions.Connectors.Market;

public interface IMarketProviderFactory
{
    IMarketProvider Create(ProviderEnvironment env);
}
