using System;
using Annium.Finance.Providers.Abstractions.Domain.Shared;
using Annium.Finance.Providers.Abstractions.Domain.User;
using Annium.Finance.Providers.Crypto.Binance.Base.User.Services;

namespace Annium.Finance.Providers.Crypto.Binance.Base.User;

/// <summary>Base configuration for a Binance account/trading connector: credentials, the user HTTP/WebSocket API endpoints, and listen key polling.</summary>
public abstract record UserConfigBase
{
    /// <summary>Gets the name of the provider to connect to.</summary>
    public required string Provider { get; init; }

    /// <summary>Gets the environment (real or test) to connect to.</summary>
    public required ProviderEnvironment Environment { get; init; }

    /// <summary>Gets the API key identifying the account to Binance.</summary>
    public required string Key { get; init; }

    /// <summary>Gets the API secret used to sign authenticated requests to Binance.</summary>
    public required string Secret { get; init; }

    /// <summary>Gets the base URI of the account/trading HTTP API.</summary>
    public required Uri HttpApi { get; init; }

    /// <summary>Gets the base URI of the user data stream WebSocket API.</summary>
    public required Uri WsApi { get; init; }

    /// <summary>Gets the relative path appended to <see cref="WsApi"/>, followed by the listen key, when opening the user data stream connection.</summary>
    public required string ListenKeyUriPath { get; init; }

    /// <summary>Gets the polling intervals used to fetch and keep the user data stream listen key alive.</summary>
    public required ListenKeyConfiguration ListenKey { get; init; }
}

/// <summary>Extension methods for converting a <see cref="UserConfigBase"/> into a <see cref="UserSettings"/>.</summary>
public static class UserConfigBaseExtensions
{
    /// <summary>Extracts the provider, environment and credentials from a user configuration into a <see cref="UserSettings"/>.</summary>
    /// <param name="config">The user configuration to extract settings from.</param>
    /// <returns>The extracted user settings.</returns>
    public static UserSettings GetSettings(this UserConfigBase config) =>
        new()
        {
            Provider = config.Provider,
            Environment = config.Environment,
            Key = config.Key,
            Secret = config.Secret,
        };
}
