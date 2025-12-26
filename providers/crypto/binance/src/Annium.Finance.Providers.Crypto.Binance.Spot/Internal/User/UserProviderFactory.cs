using Annium.Finance.Providers.Abstractions.Connectors.User;
using Annium.Finance.Providers.Abstractions.Domain.User;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Internal.User;

internal class UserProviderFactory : IUserProviderFactory
{
    public IUserProvider Create(UserSettings settings)
    {
        return new UserProvider();
    }
}
