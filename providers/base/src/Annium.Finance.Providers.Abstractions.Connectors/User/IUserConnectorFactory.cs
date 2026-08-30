using Annium.Finance.Providers.Abstractions.Domain.User;

namespace Annium.Finance.Providers.Abstractions.Connectors.User;

/// <summary>
/// Creates <see cref="IUserConnector"/> instances, resolving all their dependencies through the container
/// (used to build standalone connectors, e.g. registered as singletons in DI).
/// </summary>
public interface IUserConnectorFactory
{
    /// <summary>
    /// Creates a user connector configured with the given settings.
    /// </summary>
    /// <param name="settings">The user settings identifying the provider account to connect to.</param>
    /// <returns>A new user connector instance.</returns>
    IUserConnector Create(UserSettings settings);
}
