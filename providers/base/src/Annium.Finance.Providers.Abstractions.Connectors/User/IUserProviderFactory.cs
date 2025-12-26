using Annium.Finance.Providers.Abstractions.Domain.User;

namespace Annium.Finance.Providers.Abstractions.Connectors.User;

public interface IUserProviderFactory
{
    IUserProvider Create(UserSettings settings);
}
