using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Domain.Models;
using Annium.Finance.Providers.Tests.Shared.Connectors;
using Xunit;
using Xunit.Abstractions;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Tests.Internal.Connectors;

public class MarketProviderTests : MarketProviderTestBase
{
    public MarketProviderTests(ITestOutputHelper outputHelper)
        : base(ctx => ctx.WithBinanceUsdFutures(), "BTCUSDT", outputHelper) { }

    [Theory]
    [ClassData(typeof(ProviderMarketEnvironments))]
    public Task MarketProviderAsync(ProviderKey providerKey)
    {
        return MarketProviderBaseAsync(providerKey);
    }
}
