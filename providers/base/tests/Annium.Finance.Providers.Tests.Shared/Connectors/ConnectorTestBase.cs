using System;
using Annium.Core.DependencyInjection;
using Annium.Core.Mapper;
using Annium.Core.Runtime;
using Annium.Finance.Providers.Shared;
using Annium.Testing;
using Xunit;

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
            container.AddMapper();
            registerProvider(container.AddFinanceProviders());
        });
    }
}
