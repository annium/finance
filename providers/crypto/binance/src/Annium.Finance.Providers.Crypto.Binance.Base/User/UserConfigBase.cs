using System;
using Annium.Finance.Providers.Abstractions.Domain.Shared;
using Annium.Finance.Providers.Abstractions.Domain.User;
using Annium.Finance.Providers.Crypto.Binance.Base.User.Services;

namespace Annium.Finance.Providers.Crypto.Binance.Base.User;

public abstract record UserConfigBase
{
    public required string Provider { get; init; }
    public required ProviderEnvironment Environment { get; init; }
    public required string Key { get; init; }
    public required string Secret { get; init; }
    public required Uri HttpApi { get; init; }
    public required Uri WsApi { get; init; }
    public required string ListenKeyUriPath { get; init; }
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
