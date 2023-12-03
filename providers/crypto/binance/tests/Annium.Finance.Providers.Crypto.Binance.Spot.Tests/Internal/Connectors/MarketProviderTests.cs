using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Domain.Models;
using Annium.Finance.Providers.Tests.Shared.Connectors;
using Xunit;
using Xunit.Abstractions;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Tests.Internal.Connectors;

public class MarketProviderTests : MarketProviderTestBase
{
    public MarketProviderTests(ITestOutputHelper outputHelper)
        : base(ctx => ctx.WithBinanceSpot(), "BTCUSDT", outputHelper) { }

    [Theory]
    [ClassData(typeof(ProviderMarketEnvironments))]
    public async Task MarketProviderAsync(ProviderKey providerKey)
    {
        await MarketProviderBaseAsync(providerKey);
    }
}
