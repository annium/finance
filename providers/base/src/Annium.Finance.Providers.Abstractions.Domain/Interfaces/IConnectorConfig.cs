using Annium.Finance.Providers.Abstractions.Domain.Enums;

namespace Annium.Finance.Providers.Abstractions.Domain.Interfaces;

public interface IConnectorConfig
{
    string Provider { get; }
    ProviderEnvironment Environment { get; }
}