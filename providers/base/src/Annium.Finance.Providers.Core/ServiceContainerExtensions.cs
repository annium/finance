using Annium.Core.DependencyInjection;
using Annium.Finance.Providers.Abstractions.Connectors.Market;
using Annium.Finance.Providers.Abstractions.Connectors.User;
using Annium.Finance.Providers.Core.Internal.Market;
using Annium.Finance.Providers.Core.Internal.Shared.Status;
using Annium.Finance.Providers.Core.Internal.User;
using Annium.Finance.Providers.Core.Shared.Status;

namespace Annium.Finance.Providers.Core;

public static class ServiceContainerExtensions
{
    public static ProviderRegistrationContext AddFinanceProviders(this IServiceContainer container)
    {
        // market
        container.Add<IMarketConnectorFactory, MarketConnectorFactory>().Singleton();

        // user
        container.Add<IUserConnectorFactory, UserConnectorFactory>().Singleton();

        // status
        container.Add<StatusMonitor>().AsSelf().As<IStatusMonitor>().Scoped();
        container.Add<IStatusReporter, StatusReporter>().Transient();

        var ctx = new ProviderRegistrationContext(container);

        return ctx;
    }
}
