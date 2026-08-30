using Annium.Finance.Providers.Abstractions.Domain.User;

namespace Annium.Finance.Providers.Abstractions.Connectors.User;

/// <summary>
/// Creates <see cref="IUserConnector"/> instances whose lifetime is tied to a caller-supplied disposable box,
/// letting the caller create and dispose ad-hoc connectors outside of the standard DI lifetime.
/// </summary>
public interface IUserConnectorInstanceFactory
{
    /// <summary>
    /// Creates a user connector configured with the given settings, bound to the given disposable box.
    /// </summary>
    /// <param name="settings">The user settings identifying the provider account to connect to.</param>
    /// <param name="disposable">The disposable box the connector registers its resources with; disposing it tears the connector down.</param>
    /// <returns>A new user connector instance.</returns>
    IUserConnector Create(UserSettings settings, AsyncDisposableBox disposable);
}
