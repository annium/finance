namespace Annium.Finance.Providers.Abstractions.Domain.Shared;

/// <summary>
/// Provides identity helpers for <see cref="IConnectorSettings"/>.
/// </summary>
public static class ConnectorSettingsExtensions
{
    /// <summary>Derives the provider key (provider name and environment) these settings identify a connection with.</summary>
    /// <param name="settings">The connector settings to derive the key from.</param>
    /// <returns>A <see cref="ProviderKey"/> built from the settings' provider and environment.</returns>
    public static ProviderKey GetProviderKey(this IConnectorSettings settings) => ProviderKey.Create(settings.Provider);
}
