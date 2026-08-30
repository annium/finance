using System.Threading.Tasks;
using Annium.Finance.Providers.Core;
using Annium.Finance.Providers.Core.Shared.Loaders;
using Annium.Finance.Providers.Tests.Lib;
using Annium.Finance.Providers.Tests.Lib.User;
using Xunit;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Tests.Internal.User;

public class UserProviderTests : UserProviderTestBase
{
    public UserProviderTests(ITestOutputHelper outputHelper)
        : base(Settings.User, "BTCUSDT", outputHelper) { }

    protected override void RegisterProvider(ProviderRegistrationContext ctx)
    {
        ctx.WithBinanceUsdFutures(
            new ProviderConfiguration
            {
                ReloadContext = new CompositeLoaderConfig(200, 5, 1000, 1000, 100),
                ReloadOrders = new CompositeLoaderConfig(200, 5, 1000, 1000, 100),
                ReloadTrades = new CompositeLoaderConfig(200, 5, 1000, 1000, 100),
            }
        );
    }

    [Fact(Skip = "talks to the live exchange", SkipUnless = nameof(Exchange.IsEnabled), SkipType = typeof(Exchange))]
    public Task LoadContextAsync() => LoadContextBaseAsync();

    [Fact(Skip = "talks to the live exchange", SkipUnless = nameof(Exchange.IsEnabled), SkipType = typeof(Exchange))]
    public Task LoadOpenOrdersAsync() => LoadOpenOrdersBaseAsync();

    [Fact(Skip = "talks to the live exchange", SkipUnless = nameof(Exchange.IsEnabled), SkipType = typeof(Exchange))]
    public Task LoadLatestOrdersAsync() => LoadLatestOrdersBaseAsync();

    [Fact(Skip = "talks to the live exchange", SkipUnless = nameof(Exchange.IsEnabled), SkipType = typeof(Exchange))]
    public Task LoadHistoryOrdersAsync() => LoadHistoryOrdersBaseAsync();

    [Fact(Skip = "talks to the live exchange", SkipUnless = nameof(Exchange.IsEnabled), SkipType = typeof(Exchange))]
    public Task LoadLatestTradesAsync() => LoadLatestTradesBaseAsync();

    [Fact(Skip = "talks to the live exchange", SkipUnless = nameof(Exchange.IsEnabled), SkipType = typeof(Exchange))]
    public Task LoadHistoryTradesAsync() => LoadHistoryTradesBaseAsync();
}
