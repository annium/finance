using Annium.Finance.Providers.Abstractions.Domain.User;

namespace Annium.Finance.Providers.Abstractions.Connectors.User;

public interface IUserConnectorFactory
{
    IUserConnector Create(UserSettings settings);
}
