namespace Annium.Finance.Providers.Abstractions.Domain.Shared;

public interface IConnectorSettings
{
    string Provider { get; }
    ProviderEnvironment Environment { get; }
}
