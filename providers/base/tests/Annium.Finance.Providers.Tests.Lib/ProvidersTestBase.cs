using Annium.Core.Mapper;
using Annium.Core.Runtime;
using Annium.Finance.Providers.Core;
using Annium.Testing;
using Xunit;

namespace Annium.Finance.Providers.Tests.Lib;

public abstract class ProvidersTestBase : TestBase
{
    protected ProvidersTestBase(ITestOutputHelper outputHelper)
        : base(outputHelper)
    {
        Register(container =>
        {
            // todo: use server time
            container.AddTime().WithRealTime().SetDefault();
            container.AddMapper();
            RegisterProvider(container.AddFinanceProviders());
        });
    }

    protected virtual void RegisterProvider(ProviderRegistrationContext ctx) { }
}
