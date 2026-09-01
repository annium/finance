using System;
using Annium.Finance.Providers.Abstractions.Domain.Shared;

namespace Annium.Finance.Providers.Abstractions.Domain.User;

/// <summary>
/// Configures the connection to a provider's account (user/trading) API, including its credentials.
/// </summary>
public sealed record UserSettings : IConnectorSettings
{
    /// <summary>Gets the name of the provider to connect to.</summary>
    public string Provider { get; init; } = string.Empty;

    /// <summary>Gets the API key identifying the account to the provider.</summary>
    public string Key { get; init; } = string.Empty;

    /// <summary>Gets the API secret used to sign authenticated requests to the provider.</summary>
    public string Secret { get; init; } = string.Empty;

    /// <summary>Returns the provider and a truncated key prefix as a string, without exposing the full key or secret.</summary>
    /// <returns>The provider name followed by up to the first seven characters of <see cref="Key"/>.</returns>
    // the prefix is taken defensively: this runs from UserConnectorBase's constructor, which builds its Id
    // from it, so a key shorter than the prefix - an unset one above all - used to abort construction with
    // a range error, naming neither the setting at fault nor the account it belongs to
    public override string ToString() => $"{Provider}[{Key[..Math.Min(Key.Length, 7)]}]";
}
