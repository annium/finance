using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Connectors.User;
using Annium.Finance.Providers.Abstractions.Domain.Shared;
using Annium.Finance.Providers.Abstractions.Domain.User;
using Annium.Finance.Providers.Abstractions.Domain.User.Operations;
using Annium.Logging;
using Annium.Testing;
using NodaTime;
using Xunit;

namespace Annium.Finance.Providers.Tests.Lib.User;

/// <summary>
/// Base for tests that resolve a provider's user data provider (request/response, not streaming) for a real
/// account and check that it can load context, orders and trades. Read-only: it never places orders itself,
/// though it does authenticate against a real account.
/// </summary>
[Trait(TestBlock.Name, TestBlock.Read)]
public abstract class UserProviderTestBase : ProvidersTestBase
{
    /// <summary>The account credentials/environment to resolve the user provider for.</summary>
    private readonly UserSettings _settings;

    /// <summary>The symbol the derived test loads orders and trades for.</summary>
    private readonly string _symbol;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserProviderTestBase"/> class.
    /// </summary>
    /// <param name="settings">The account credentials/environment to resolve the user provider for.</param>
    /// <param name="symbol">The symbol to load orders and trades for.</param>
    /// <param name="outputHelper">The xUnit output helper to route trace logging to.</param>
    protected UserProviderTestBase(UserSettings settings, string symbol, ITestOutputHelper outputHelper)
        : base(outputHelper)
    {
        _settings = settings;
        _symbol = symbol;
    }

    /// <summary>
    /// Loads the account's context and asserts it succeeded and reports at least one asset.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected async Task LoadContextBaseAsync()
    {
        this.Trace("start");

        var provider = ResolveProvider();
        var context = await provider.LoadContextAsync();

        context.Status.Is(UserOperationStatus.Ok);
        var ctx = context.Data.NotNull();
        ctx.Assets.Count.IsGreater(0);

        this.Trace("done");
    }

    /// <summary>
    /// Loads the account's currently open orders and asserts the call succeeded.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected async Task LoadOpenOrdersBaseAsync()
    {
        this.Trace("start");

        var provider = ResolveProvider();
        var openOrders = await provider.LoadOpenOrdersAsync();

        openOrders.Status.Is(UserOperationStatus.Ok);
        openOrders.Data.NotNull();

        this.Trace("done");
    }

    /// <summary>
    /// Loads the configured symbol's most recent orders (no time bound) and asserts the call succeeded.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected async Task LoadLatestOrdersBaseAsync()
    {
        this.Trace("start");

        var provider = ResolveProvider();
        var orders = await provider.LoadOrdersAsync(_symbol, null);

        orders.Status.Is(UserOperationStatus.Ok);
        orders.Data.NotNull();

        this.Trace("done");
    }

    /// <summary>
    /// Loads the configured symbol's orders since <see cref="GetSince"/> and asserts the call succeeded.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected async Task LoadHistoryOrdersBaseAsync()
    {
        this.Trace("start");

        var provider = ResolveProvider();
        var historicalOrders = await provider.LoadOrdersAsync(_symbol, GetSince());

        historicalOrders.Status.Is(UserOperationStatus.Ok);
        historicalOrders.Data.NotNull();

        this.Trace("done");
    }

    /// <summary>
    /// Loads the configured symbol's most recent trades (no time bound) and asserts the call succeeded.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected async Task LoadLatestTradesBaseAsync()
    {
        this.Trace("start");

        var provider = ResolveProvider();
        var trades = await provider.LoadTradesAsync(_symbol, null);

        trades.Status.Is(UserOperationStatus.Ok);
        trades.Data.NotNull();

        this.Trace("done");
    }

    /// <summary>
    /// Loads the configured symbol's trades since <see cref="GetSince"/> and asserts the call succeeded.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected async Task LoadHistoryTradesBaseAsync()
    {
        this.Trace("start");

        var provider = ResolveProvider();
        var historicalTrades = await provider.LoadTradesAsync(_symbol, GetSince());

        historicalTrades.Status.Is(UserOperationStatus.Ok);
        historicalTrades.Data.NotNull();

        this.Trace("done");
    }

    /// <summary>
    /// Resolves the user data provider for the configured account's provider/environment.
    /// </summary>
    /// <returns>The resolved user data provider.</returns>
    private IUserProvider ResolveProvider()
    {
        var providerKey = _settings.GetProviderKey();
        var keys = Get<IEnumerable<ProviderKey>>().ToArray();
        keys.Contains(providerKey).IsTrue();

        var providerFactory = GetKeyed<IUserProviderFactory>(providerKey.Provider);
        return providerFactory.Create(_settings);
    }

    /// <summary>
    /// Computes the timestamp one day before now, used as the lower bound for the history queries.
    /// </summary>
    /// <returns>A moment one day before now, in Unix milliseconds.</returns>
    private long GetSince()
    {
        var now = Get<ITimeProvider>().Now;

        return (now - Duration.FromDays(1)).ToUnixTimeMilliseconds();
    }
}
