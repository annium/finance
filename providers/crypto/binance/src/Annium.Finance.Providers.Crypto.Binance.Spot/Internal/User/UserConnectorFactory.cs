using System;
using Annium.Core.DependencyInjection;
using Annium.Core.Mapper;
using Annium.Finance.Providers.Abstractions.Connectors.User;
using Annium.Finance.Providers.Abstractions.Domain.User;
using Annium.Finance.Providers.Core.Shared.Status;
using Annium.Finance.Providers.Core.User;
using Annium.Logging;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Internal.User;

internal class UserConnectorFactory(IServiceProvider sp) : IUserConnectorInstanceFactory
{
    public IUserConnector Create(UserSettings settings, AsyncDisposableBox disposable)
    {
        var config = sp.Resolve<IMapper>().Map<UserConfig>(settings);

        var provider = sp.CreateUserProvider(settings);
        var reporter = sp.Resolve<IStatusReporter>();
        var monitor = sp.Resolve<IStatusMonitor>();
        var logger = sp.Resolve<ILogger>();

        return new UserConnector(config, provider, reporter, monitor, disposable, logger);
    }
}
