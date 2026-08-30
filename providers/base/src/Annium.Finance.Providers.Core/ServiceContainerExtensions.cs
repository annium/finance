using Annium.Core.DependencyInjection;
using Annium.Finance.Providers.Abstractions.Connectors.Market;
using Annium.Finance.Providers.Abstractions.Connectors.User;
using Annium.Finance.Providers.Core.Internal.Market;
using Annium.Finance.Providers.Core.Internal.Shared.Status;
using Annium.Finance.Providers.Core.Internal.User;
using Annium.Finance.Providers.Core.Shared.Status;

namespace Annium.Finance.Providers.Core;

/// <summary>
/// Entry point extension methods for registering finance providers into an <see cref="IServiceContainer"/>.
/// </summary>
public static class ServiceContainerExtensions
{
    /// <summary>
    /// Registers the shared services every finance provider depends on (connector factories, status
    /// monitoring/reporting), and returns a context for registering individual providers via
    /// <see cref="ProviderRegistrationContext.AddProvider{TMarketProviderFactory, TMarketConnectorFactory, TUserProviderFactory, TUserConnectorFactory, TFinanceService}"/>.
    /// </summary>
    /// <param name="container">The container to register services into.</param>
    /// <returns>A registration context bound to <paramref name="container"/>.</returns>
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
