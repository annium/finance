using Annium.Finance.Providers.Abstractions.Domain.User;

namespace Annium.Finance.Providers.Abstractions.Connectors.User;

/// <summary>
/// Creates <see cref="IUserProvider"/> instances for a given user configuration.
/// </summary>
public interface IUserProviderFactory
{
    /// <summary>
    /// Creates a user provider configured with the given settings.
    /// </summary>
    /// <param name="settings">The user settings identifying the provider account to connect to.</param>
    /// <returns>A new user provider instance.</returns>
    IUserProvider Create(UserSettings settings);
}
