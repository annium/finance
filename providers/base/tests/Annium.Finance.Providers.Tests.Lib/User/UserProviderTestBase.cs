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

public abstract class UserProviderTestBase : ProvidersTestBase
{
    private readonly UserSettings _settings;
    private readonly string _symbol;

    protected UserProviderTestBase(UserSettings settings, string symbol, ITestOutputHelper outputHelper)
        : base(outputHelper)
    {
        _settings = settings;
        _symbol = symbol;
    }

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

    protected async Task LoadOpenOrdersBaseAsync()
    {
        this.Trace("start");

        var provider = ResolveProvider();
        var openOrders = await provider.LoadOpenOrdersAsync();

        openOrders.Status.Is(UserOperationStatus.Ok);
        openOrders.Data.NotNull();

        this.Trace("done");
    }

    protected async Task LoadLatestOrdersBaseAsync()
    {
        this.Trace("start");

        var provider = ResolveProvider();
        var orders = await provider.LoadOrdersAsync(_symbol, null);

        orders.Status.Is(UserOperationStatus.Ok);
        orders.Data.NotNull();

        this.Trace("done");
    }

    protected async Task LoadHistoryOrdersBaseAsync()
    {
        this.Trace("start");

        var provider = ResolveProvider();
        var historicalOrders = await provider.LoadOrdersAsync(_symbol, GetSince());

        historicalOrders.Status.Is(UserOperationStatus.Ok);
        historicalOrders.Data.NotNull();

        this.Trace("done");
    }

    protected async Task LoadLatestTradesBaseAsync()
    {
        this.Trace("start");

        var provider = ResolveProvider();
        var trades = await provider.LoadTradesAsync(_symbol, null);

        trades.Status.Is(UserOperationStatus.Ok);
        trades.Data.NotNull();

        this.Trace("done");
    }

    protected async Task LoadHistoryTradesBaseAsync()
    {
        this.Trace("start");

        var provider = ResolveProvider();
        var historicalTrades = await provider.LoadTradesAsync(_symbol, GetSince());

        historicalTrades.Status.Is(UserOperationStatus.Ok);
        historicalTrades.Data.NotNull();

        this.Trace("done");
    }

    private IUserProvider ResolveProvider()
    {
        var providerKey = _settings.GetProviderKey();
        var keys = Get<IEnumerable<ProviderKey>>().ToArray();
        keys.Contains(providerKey).IsTrue();

        var providerFactory = GetKeyed<IUserProviderFactory>(providerKey.Provider);
        return providerFactory.Create(_settings);
    }

    private long GetSince()
    {
        var now = Get<ITimeProvider>().Now;

        return (now - Duration.FromDays(1)).ToUnixTimeMilliseconds();
    }
}
