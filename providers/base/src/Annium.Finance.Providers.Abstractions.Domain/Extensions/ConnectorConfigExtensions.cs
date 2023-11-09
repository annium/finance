using Annium.Finance.Providers.Abstractions.Domain.Interfaces;
using Annium.Finance.Providers.Abstractions.Domain.Models;

namespace Annium.Finance.Providers.Abstractions.Domain.Extensions;

public static class ConnectorConfigExtensions
{
    public static ProviderKey GetProviderKey(this IConnectorConfig config) =>
        ProviderKey.Create(config.Provider, config.Environment);
}
