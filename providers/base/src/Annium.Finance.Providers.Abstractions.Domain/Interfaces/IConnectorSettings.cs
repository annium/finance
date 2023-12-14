using Annium.Finance.Providers.Abstractions.Domain.Enums;

namespace Annium.Finance.Providers.Abstractions.Domain.Interfaces;

public interface IConnectorSettings
{
    string Provider { get; }
    ProviderEnvironment Environment { get; }
}
