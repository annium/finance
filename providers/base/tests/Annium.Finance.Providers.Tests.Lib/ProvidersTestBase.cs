using System;
using Annium.Core.Mapper;
using Annium.Core.Runtime;
using Annium.Finance.Providers.Shared;
using Annium.Testing;
using Xunit;

namespace Annium.Finance.Providers.Tests.Lib;

public abstract class ProvidersTestBase : TestBase
{
    protected ProvidersTestBase(ITestOutputHelper outputHelper)
        : this(_ => { }, outputHelper) { }

    protected ProvidersTestBase(Action<ProviderRegistrationContext> registerProvider, ITestOutputHelper outputHelper)
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
