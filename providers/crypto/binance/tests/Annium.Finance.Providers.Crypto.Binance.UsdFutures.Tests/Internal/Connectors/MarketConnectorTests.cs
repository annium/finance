using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Domain.Models;
using Annium.Finance.Providers.Tests.Shared.Connectors;
using Xunit;
using Xunit.Abstractions;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Tests.Internal.Connectors;

public class MarketConnectorTests : MarketConnectorTestBase
{
    public MarketConnectorTests(ITestOutputHelper outputHelper)
        : base(ctx => ctx.WithBinanceUsdFutures(), "BTCUSDT", outputHelper) { }

    [Theory]
    [ClassData(typeof(ProviderMarketEnvironments))]
    public Task MarketConnectorAsync(ProviderKey providerKey)
    {
        return MarketConnectorBaseAsync(providerKey);
    }
}
