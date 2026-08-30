using Annium.Finance.Providers.Abstractions.Connectors.User;
using Annium.Finance.Providers.Abstractions.Domain.User;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Internal.User;

/// <summary>Builds Binance spot <see cref="UserProvider"/> instances.</summary>
internal class UserProviderFactory : IUserProviderFactory
{
    /// <summary>Creates a new Binance spot user provider.</summary>
    /// <param name="settings">The account connection settings; currently unused, as the provider is not yet implemented.</param>
    /// <returns>The created user provider.</returns>
    public IUserProvider Create(UserSettings settings)
    {
        return new UserProvider();
    }
}
