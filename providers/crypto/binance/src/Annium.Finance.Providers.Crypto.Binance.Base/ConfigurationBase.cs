using System;
using Annium.Finance.Providers.Abstractions.Domain.Enums;

namespace Annium.Finance.Providers.Crypto.Binance.Base;

public abstract record ConfigurationBase
{
    public required string Provider { get; init; }
    public required ProviderEnvironment Environment { get; init; }
    public required Uri HttpApi { get; init; }
    public required Uri WsApi { get; init; }
    public required string WsMarketEndpoint { get; init; }
}
