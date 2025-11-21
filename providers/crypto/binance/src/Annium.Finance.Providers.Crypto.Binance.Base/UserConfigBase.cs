using System;
using Annium.Finance.Providers.Abstractions.Domain.Enums;
using Annium.Finance.Providers.Abstractions.Domain.Models;

namespace Annium.Finance.Providers.Crypto.Binance.Base;

public abstract record UserConfigBase
{
    public required string Provider { get; init; }
    public required ProviderEnvironment Environment { get; init; }
    public required string Key { get; init; }
    public required string Secret { get; init; }
    public required Uri HttpApi { get; init; }
    public required Uri WsApi { get; init; }
    public required string ListenKeyBase { get; init; }
    public required ListenKeyConfiguration ListenKey { get; init; }
}

public static class UserConfigBaseExtensions
{
    public static UserSettings GetSettings(this UserConfigBase config) =>
        new()
        {
            Provider = config.Provider,
            Environment = config.Environment,
            Key = config.Key,
            Secret = config.Secret,
        };
}
