using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Domain.Extensions;
using Annium.Finance.Providers.Tests.Shared.Connectors;
using Xunit;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Tests.Internal.Connectors;

public class MarketConnectorTests : MarketConnectorTestBase
{
    public MarketConnectorTests(ITestOutputHelper outputHelper)
        : base(ctx => ctx.WithBinanceSpot(), "BTCUSDT", outputHelper) { }

    [Fact]
    public Task MarketConnectorAsync()
    {
        return MarketConnectorBaseAsync(Settings.Market.GetProviderKey());
    }
}
