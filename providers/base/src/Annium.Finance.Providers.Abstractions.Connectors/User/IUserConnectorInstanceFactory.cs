using Annium.Finance.Providers.Abstractions.Domain.User;

namespace Annium.Finance.Providers.Abstractions.Connectors.User;

public interface IUserConnectorInstanceFactory
{
    IUserConnector Create(UserSettings settings, AsyncDisposableBox disposable);
}
