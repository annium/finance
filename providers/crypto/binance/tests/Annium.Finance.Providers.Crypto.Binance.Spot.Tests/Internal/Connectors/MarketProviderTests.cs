using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Domain.Extensions;
using Annium.Finance.Providers.Shared;
using Annium.Finance.Providers.Tests.Lib.Connectors;
using Xunit;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Tests.Internal.Connectors;

public class MarketProviderTests : MarketProviderTestBase
{
    public MarketProviderTests(ITestOutputHelper outputHelper)
        : base("BTCUSDT", outputHelper) { }

    protected override void RegisterProvider(ProviderRegistrationContext ctx)
    {
        ctx.WithBinanceSpot();
    }

    [Fact]
    public Task MarketProviderAsync()
    {
        return MarketProviderBaseAsync(Settings.Market.GetProviderKey());
    }
}
