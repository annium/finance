using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Domain.Enums;
using Annium.Finance.Providers.Abstractions.Domain.Extensions;
using Annium.Finance.Providers.Abstractions.Domain.Models;
using Annium.Finance.Providers.Shared.Services;
using Annium.Finance.Providers.Tests.Shared.Connectors;
using Annium.Logging;
using Xunit;
using Xunit.Abstractions;
using static Annium.Finance.Providers.Abstractions.Domain.Tools.RequestBuilder;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Tests.Internal.Connectors;

public class UserConnectorTests : UserConnectorPositionalTestBase
{
    private decimal ExtremeHighQty => Instrument.MaxQty * 1_000_000;
    private decimal LowPrice => Ticker.Price() * 0.7m;

    public UserConnectorTests(ITestOutputHelper output)
        : base(
            ctx =>
                ctx.WithBinanceUsdFutures(
                    new ProviderConfiguration
                    {
                        ReloadAccount = new CompositeLoaderConfig(200, 5, 1000, 1000, 100),
                        ReloadOrders = new CompositeLoaderConfig(200, 5, 1000, 1000, 100),
                        ReloadTrades = new CompositeLoaderConfig(200, 5, 1000, 1000, 100),
                    }
                ),
            new UserSettings(
                Constants.Provider,
                ProviderEnvironment.Test,
                "19136244bcbe0adb854f5234451ddf80c440ca7372fde16cb06178900712e8ba",
                "493495031de246dd8cfbcb3a3676df563c99abaf1240105af34567d440c1406e"
            ),
            "BTCUSDT",
            output
        ) { }

    [Fact]
    public async Task InitOrder_Invalid()
    {
        this.Trace("start");

        var request = InitLimitOrder(
            GenerateClientOrderId(),
            Instrument.Symbol,
            OrderSide.Buy,
            ExtremeHighQty,
            LowPrice
        );
        await InitInvalidOrder(request);

        this.Trace("done");
    }

    [Fact]
    public async Task CancelAllOrders()
    {
        this.Trace("start");

        await CancelOpenOrders();

        this.Trace("done");
    }
}
