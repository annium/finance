using System;
using Annium.Finance.Providers.Abstractions.Domain.Market;
using Annium.Finance.Providers.Abstractions.Domain.Shared;

namespace Annium.Finance.Providers.Crypto.Binance.Base.Market;

/// <summary>Base configuration for a Binance market-data connector: the provider identity, environment, and the market HTTP/WebSocket API endpoints.</summary>
public abstract record MarketConfigBase
{
    /// <summary>Gets the name of the provider to connect to.</summary>
    public required string Provider { get; init; }

    /// <summary>Gets the base URI of the market HTTP API.</summary>
    public required Uri HttpApi { get; init; }

    /// <summary>Gets the base URI of the market WebSocket API.</summary>
    public required Uri WsApi { get; init; }

    /// <summary>Gets the relative path appended to <see cref="WsApi"/> when opening a market WebSocket connection.</summary>
    public required string WsUriPath { get; init; }
}

/// <summary>Extension methods for converting a <see cref="MarketConfigBase"/> into a <see cref="MarketSettings"/>.</summary>
public static class UserConfigBaseExtensions
{
    /// <summary>Extracts the provider from a market configuration into a <see cref="MarketSettings"/>.</summary>
    /// <param name="config">The market configuration to extract settings from.</param>
    /// <returns>The extracted market settings.</returns>
    public static MarketSettings GetSettings(this MarketConfigBase config) => new() { Provider = config.Provider };
}
