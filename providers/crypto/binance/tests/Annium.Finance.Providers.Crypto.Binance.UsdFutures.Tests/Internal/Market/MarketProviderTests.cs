using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Domain.Shared;
using Annium.Finance.Providers.Core;
using Annium.Finance.Providers.Tests.Lib;
using Annium.Finance.Providers.Tests.Lib.Market;
using Xunit;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Tests.Internal.Market;

/// <summary>
/// Runs <see cref="MarketProviderTestBase.MarketProviderBaseAsync"/> against the real Binance USD-M futures
/// market provider for BTCUSDT: it loads context and two days of one-minute candles and asserts the count and
/// data look right. Read-only, but it does call the real exchange, so it runs only when
/// <see cref="Exchange.IsEnabled"/> is set.
/// </summary>
public class MarketProviderTests : MarketProviderTestBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MarketProviderTests"/> class, targeting BTCUSDT.
    /// </summary>
    /// <param name="outputHelper">The xUnit output helper to route trace logging to.</param>
    public MarketProviderTests(ITestOutputHelper outputHelper)
        : base("BTCUSDT", outputHelper) { }

    /// <summary>
    /// Registers the Binance USD-M futures provider so the provider under test is resolved from its actual registration.
    /// </summary>
    /// <param name="ctx">The fluent context to register providers into.</param>
    protected override void RegisterProvider(ProviderRegistrationContext ctx)
    {
        ctx.WithBinanceUsdFutures();
    }

    /// <summary>
    /// Loads context and two days of one-minute BTCUSDT candles from the live USD-M futures market provider
    /// and asserts the expected candle count. Talks to the real exchange; skipped unless
    /// <see cref="Exchange.IsEnabled"/> is set.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact(Skip = "talks to the live exchange", SkipUnless = nameof(Exchange.IsEnabled), SkipType = typeof(Exchange))]
    public Task MarketProviderAsync()
    {
        return MarketProviderBaseAsync(Settings.Market.GetProviderKey());
    }
}
