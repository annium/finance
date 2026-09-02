using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Domain.Shared;
using Annium.Finance.Providers.Core;
using Annium.Finance.Providers.Tests.Lib;
using Annium.Finance.Providers.Tests.Lib.Market;
using Xunit;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Tests.Internal.Market;

/// <summary>
/// Runs <see cref="MarketConnectorTestBase.MarketConnectorBaseAsync"/> against the real Binance USD-M futures
/// market connector for BTCUSDT: it connects, subscribes to the ticker stream, and asserts instrument metadata
/// and live prices come through. Read-only, but it does open a real connection to Binance, so it runs only
/// when the read block is asked for.
/// </summary>
[Collection(ExchangeCollection.Name)]
public class MarketConnectorTests : MarketConnectorTestBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MarketConnectorTests"/> class, targeting BTCUSDT.
    /// </summary>
    /// <param name="outputHelper">The xUnit output helper to route trace logging to.</param>
    public MarketConnectorTests(ITestOutputHelper outputHelper)
        : base("BTCUSDT", outputHelper) { }

    /// <summary>
    /// Registers the Binance USD-M futures provider so the connector under test is resolved from its actual registration.
    /// </summary>
    /// <param name="ctx">The fluent context to register providers into.</param>
    protected override void RegisterProvider(ProviderRegistrationContext ctx)
    {
        ctx.WithBinanceUsdFutures();
    }

    /// <summary>
    /// Connects the live USD-M futures market connector and asserts it reports instrument metadata and a
    /// ticker for BTCUSDT. Talks to the real exchange; in the read block.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public Task MarketConnectorAsync()
    {
        return MarketConnectorBaseAsync(Settings.Market.GetProviderKey());
    }
}
