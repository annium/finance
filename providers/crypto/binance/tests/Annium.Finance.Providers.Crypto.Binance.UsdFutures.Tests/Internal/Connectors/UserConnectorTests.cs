using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Domain.Enums;
using Annium.Finance.Providers.Abstractions.Domain.Models;
using Annium.Finance.Providers.Tests.Shared.Connectors;
using Annium.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Tests.Internal.Connectors;

public class UserConnectorTests : UserConnectorPositionalTestBase
{
    public UserConnectorTests(ITestOutputHelper output)
        : base(
            ctx =>
                ctx.WithBinanceUsdFutures(
                    new ProviderConfiguration
                    {
                        ReloadAccountInterval = 1_000,
                        ReloadAccountDebounce = 100,
                        ReloadOrdersInterval = 10_000,
                        ReloadOrdersDebounce = 100,
                        ReloadDealsDebounce = 100
                    }
                ),
            new UserSettings(
                Constants.Provider,
                ProviderEnvironment.Test,
                "19136244bcbe0adb854f5234451ddf80c440ca7372fde16cb06178900712e8ba",
                "493495031de246dd8cfbcb3a3676df563c99abaf1240105af34567d440c1406e"
            ),
            "MATICUSDT",
            output
        ) { }

    [Fact]
    public async Task CancelAllOrders()
    {
        this.Trace("start");

        await CancelOpenOrders();

        this.Trace("done");
    }
}
