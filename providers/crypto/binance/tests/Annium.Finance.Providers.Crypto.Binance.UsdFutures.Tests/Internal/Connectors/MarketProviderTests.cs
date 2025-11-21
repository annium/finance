using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Domain.Extensions;
using Annium.Finance.Providers.Tests.Shared.Connectors;
using Xunit;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Tests.Internal.Connectors;

public class MarketProviderTests : MarketProviderTestBase
{
    public MarketProviderTests(ITestOutputHelper outputHelper)
        : base(ctx => ctx.WithBinanceUsdFutures(), "BTCUSDT", outputHelper) { }

    [Fact]
    public Task MarketProviderAsync()
    {
        return MarketProviderBaseAsync(Settings.Market.GetProviderKey());
    }
}
