using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Domain.Shared;
using Annium.Finance.Providers.Core;
using Annium.Finance.Providers.Tests.Lib.Market;
using Xunit;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Tests.Internal.Connectors;

public class MarketProviderTests : MarketProviderTestBase
{
    public MarketProviderTests(ITestOutputHelper outputHelper)
        : base("BTCUSDT", outputHelper) { }

    protected override void RegisterProvider(ProviderRegistrationContext ctx)
    {
        ctx.WithBinanceUsdFutures();
    }

    [Fact]
    public Task MarketProviderAsync()
    {
        return MarketProviderBaseAsync(Settings.Market.GetProviderKey());
    }
}
