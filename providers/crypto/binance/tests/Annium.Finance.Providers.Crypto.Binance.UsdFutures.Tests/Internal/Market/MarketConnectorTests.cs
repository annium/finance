using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Domain.Shared;
using Annium.Finance.Providers.Core;
using Annium.Finance.Providers.Tests.Lib.Market;
using Xunit;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Tests.Internal.Market;

public class MarketConnectorTests : MarketConnectorTestBase
{
    public MarketConnectorTests(ITestOutputHelper outputHelper)
        : base("BTCUSDT", outputHelper) { }

    protected override void RegisterProvider(ProviderRegistrationContext ctx)
    {
        ctx.WithBinanceUsdFutures();
    }

    [Fact]
    public Task MarketConnectorAsync()
    {
        return MarketConnectorBaseAsync(Settings.Market.GetProviderKey());
    }
}
