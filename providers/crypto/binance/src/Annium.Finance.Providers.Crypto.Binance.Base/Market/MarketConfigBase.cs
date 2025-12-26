using System;
using Annium.Finance.Providers.Abstractions.Domain.Market;
using Annium.Finance.Providers.Abstractions.Domain.Shared;

namespace Annium.Finance.Providers.Crypto.Binance.Base.Market;

public abstract record MarketConfigBase
{
    public required string Provider { get; init; }
    public required ProviderEnvironment Environment { get; init; }
    public required Uri HttpApi { get; init; }
    public required Uri WsApi { get; init; }
    public required string WsUriPath { get; init; }
}

public static class UserConfigBaseExtensions
{
    public static MarketSettings GetSettings(this MarketConfigBase config) =>
        new() { Provider = config.Provider, Environment = config.Environment };
}
