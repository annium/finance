using Annium.Finance.Providers.Abstractions.Domain.Enums;
using Annium.Finance.Providers.Abstractions.Domain.Models;

namespace Annium.Finance.Providers.Crypto.Binance.Base;

public abstract record UserConfigBase
{
    public required string Provider { get; init; }
    public required ProviderEnvironment Environment { get; init; }
    public required string Key { get; init; }
    public required string Secret { get; init; }
}

public static class UserConfigBaseExtensions
{
    public static UserSettings GetSettings(this UserConfigBase config) =>
        new UserSettings(config.Provider, config.Environment, config.Key, config.Secret);
}
