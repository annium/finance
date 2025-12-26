namespace Annium.Finance.Providers.Abstractions.Domain.Shared;

public static class ConnectorSettingsExtensions
{
    public static ProviderKey GetProviderKey(this IConnectorSettings settings) =>
        ProviderKey.Create(settings.Provider, settings.Environment);
}
