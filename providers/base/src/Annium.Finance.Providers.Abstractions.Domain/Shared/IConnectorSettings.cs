namespace Annium.Finance.Providers.Abstractions.Domain.Shared;

/// <summary>
/// Represents the minimal settings needed to identify a connection to a provider: which provider, and which environment.
/// </summary>
public interface IConnectorSettings
{
    /// <summary>Gets the name of the provider to connect to.</summary>
    string Provider { get; }

    /// <summary>Gets the environment (real or test) to connect to.</summary>
    ProviderEnvironment Environment { get; }
}
