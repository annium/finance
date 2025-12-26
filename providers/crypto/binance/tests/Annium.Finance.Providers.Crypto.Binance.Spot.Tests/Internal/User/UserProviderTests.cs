using System.Threading.Tasks;
using Annium.Finance.Providers.Core;
using Annium.Finance.Providers.Core.Shared.Loaders;
using Annium.Finance.Providers.Tests.Lib.User;
using Xunit;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Tests.Internal.User;

public class UserProviderTests : UserProviderTestBase
{
    public UserProviderTests(ITestOutputHelper outputHelper)
        : base(Settings.User, "BTCUSDT", outputHelper) { }

    protected override void RegisterProvider(ProviderRegistrationContext ctx)
    {
        ctx.WithBinanceSpot(
            new ProviderConfiguration
            {
                ReloadContext = new CompositeLoaderConfig(200, 5, 1000, 1000, 100),
                ReloadOrders = new CompositeLoaderConfig(200, 5, 1000, 1000, 100),
                ReloadTrades = new CompositeLoaderConfig(200, 5, 1000, 1000, 100),
            }
        );
    }

    [Fact(Skip = "Not implemented")]
    public Task LoadContextAsync() => LoadContextBaseAsync();

    [Fact(Skip = "Not implemented")]
    public Task LoadOpenOrdersAsync() => LoadOpenOrdersBaseAsync();

    [Fact(Skip = "Not implemented")]
    public Task LoadLatestOrdersAsync() => LoadLatestOrdersBaseAsync();

    [Fact(Skip = "Not implemented")]
    public Task LoadHistoryOrdersAsync() => LoadHistoryOrdersBaseAsync();

    [Fact(Skip = "Not implemented")]
    public Task LoadLatestTradesAsync() => LoadLatestTradesBaseAsync();

    [Fact(Skip = "Not implemented")]
    public Task LoadHistoryTradesAsync() => LoadHistoryTradesBaseAsync();
}
