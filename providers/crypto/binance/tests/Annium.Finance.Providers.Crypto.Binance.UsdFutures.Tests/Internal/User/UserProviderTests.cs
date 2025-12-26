using System.Threading.Tasks;
using Annium.Finance.Providers.Core;
using Annium.Finance.Providers.Core.Shared.Loaders;
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

    [Fact]
    public Task LoadContextAsync() => LoadContextBaseAsync();

    [Fact]
    public Task LoadOpenOrdersAsync() => LoadOpenOrdersBaseAsync();

    [Fact]
    public Task LoadLatestOrdersAsync() => LoadLatestOrdersBaseAsync();

    [Fact]
    public Task LoadHistoryOrdersAsync() => LoadHistoryOrdersBaseAsync();

    [Fact]
    public Task LoadLatestTradesAsync() => LoadLatestTradesBaseAsync();

    [Fact]
    public Task LoadHistoryTradesAsync() => LoadHistoryTradesBaseAsync();
}
