using System;

namespace Annium.Finance.Providers.Abstractions.Domain.Shared;

/// <summary>
/// Uniquely identifies a provider connection by provider name, for use as a lookup key across connectors.
/// </summary>
public sealed record ProviderKey
{
    /// <summary>Creates a provider key for the given provider.</summary>
    /// <param name="provider">The name of the provider.</param>
    /// <returns>A <see cref="ProviderKey"/> for the given provider.</returns>
    public static ProviderKey Create(string provider) => new(provider);

    /// <summary>Gets the name of the provider.</summary>
    public string Provider { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProviderKey"/> class.
    /// </summary>
    /// <param name="provider">The name of the provider.</param>
    private ProviderKey(string provider)
    {
        Provider = provider;
    }

    /// <summary>Returns the provider name.</summary>
    /// <returns>The provider name.</returns>
    public override string ToString() => Provider;

    /// <summary>Computes a hash code from the provider name.</summary>
    /// <returns>A hash code for <see cref="Provider"/>.</returns>
    public override int GetHashCode() => Provider.GetHashCode();
}
