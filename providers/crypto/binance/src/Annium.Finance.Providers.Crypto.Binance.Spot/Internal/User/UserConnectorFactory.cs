using System;
using Annium.Core.DependencyInjection;
using Annium.Core.Mapper;
using Annium.Finance.Providers.Abstractions.Connectors.User;
using Annium.Finance.Providers.Abstractions.Domain.User;
using Annium.Finance.Providers.Core.Shared.Status;
using Annium.Finance.Providers.Core.User;
using Annium.Logging;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Internal.User;

/// <summary>Builds Binance spot <see cref="UserConnector"/> instances, resolving their dependencies from the container.</summary>
/// <param name="sp">The service provider used to resolve dependencies.</param>
internal class UserConnectorFactory(IServiceProvider sp) : IUserConnectorInstanceFactory
{
    /// <summary>Creates a new Binance spot user connector for the given settings.</summary>
    /// <param name="settings">The account connection settings to configure the connector with.</param>
    /// <param name="disposable">The disposable box the connector will register its cleanup actions on.</param>
    /// <returns>The created user connector.</returns>
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
