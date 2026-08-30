using Annium.Core.Mapper;
using Annium.Core.Runtime;
using Annium.Finance.Providers.Core;
using Annium.Testing;
using Xunit;

namespace Annium.Finance.Providers.Tests.Lib;

/// <summary>
/// Root of the provider test hierarchy. Wires real-time clock, mapper and finance-provider registration into
/// the DI container; derived bases override <see cref="RegisterProvider"/> to register the specific provider
/// under test.
/// </summary>
public abstract class ProvidersTestBase : TestBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProvidersTestBase"/> class.
    /// </summary>
    /// <param name="outputHelper">The xUnit output helper to route trace logging to.</param>
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

    /// <summary>
    /// Registers the finance provider(s) a derived test needs. The base implementation registers none.
    /// </summary>
    /// <param name="ctx">The fluent context to register providers into.</param>
    protected virtual void RegisterProvider(ProviderRegistrationContext ctx) { }
}
