using System;
using Annium.Core.DependencyInjection;
using Annium.Finance.Providers.Shared;
using Annium.Testing;
using Xunit.Abstractions;

namespace Annium.Finance.Providers.Tests.Shared.Connectors;

public abstract class ConnectorTestBase : TestBase
{
    protected ConnectorTestBase(Action<ProviderRegistrationContext> registerProvider, ITestOutputHelper outputHelper)
        : base(outputHelper)
    {
        Register(container =>
        {
            // todo: use server time
            container.AddTime().WithRealTime().SetDefault();
            container.AddScheduler();
            container.AddTables();
            container.AddMapper();
            registerProvider(container.AddProviders(ServiceLifetime.Singleton));
        });
    }
}
