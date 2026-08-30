using System;

namespace Annium.Finance.Providers.Abstractions.Domain.Shared;

/// <summary>
/// Uniquely identifies a provider connection by provider name and environment, for use as a lookup key across connectors.
/// </summary>
public sealed record ProviderKey
{
    /// <summary>Creates a provider key for the given provider and environment.</summary>
    /// <param name="provider">The name of the provider.</param>
    /// <param name="environment">The environment (real or test) the key identifies.</param>
    /// <returns>A <see cref="ProviderKey"/> for the given provider and environment.</returns>
    public static ProviderKey Create(string provider, ProviderEnvironment environment) => new(provider, environment);

    /// <summary>Gets the name of the provider.</summary>
    public string Provider { get; }

    /// <summary>Gets the environment (real or test) this key identifies.</summary>
    public ProviderEnvironment Environment { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProviderKey"/> class.
    /// </summary>
    /// <param name="provider">The name of the provider.</param>
    /// <param name="environment">The environment (real or test) the key identifies.</param>
    private ProviderKey(string provider, ProviderEnvironment environment)
    {
        Provider = provider;
        Environment = environment;
    }

    /// <summary>Returns the provider and environment as a string.</summary>
    /// <returns>A string in the form "Provider[Environment]".</returns>
    public override string ToString() => $"{Provider}[{Environment}]";

    /// <summary>Computes a hash code from the provider name and environment.</summary>
    /// <returns>A hash code combining <see cref="Provider"/> and <see cref="Environment"/>.</returns>
    public override int GetHashCode() => HashCode.Combine(Provider.GetHashCode(), (int)Environment);
}
