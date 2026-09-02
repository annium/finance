using System.Threading.Tasks;
using Annium.Finance.Providers.Core;
using Annium.Finance.Providers.Core.Shared.Loaders;
using Annium.Finance.Providers.Tests.Lib;
using Annium.Finance.Providers.Tests.Lib.User;
using Xunit;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Tests.Internal.User;

/// <summary>
/// Runs <see cref="UserProviderTestBase"/>'s context/orders/trades checks against the real Binance USD-M
/// futures user provider for BTCUSDT, authenticating with the account in <see cref="Settings.User"/>.
/// Read-only, but it does authenticate against a real account, so every case runs only when
/// the read block is asked for.
/// </summary>
[Collection(ExchangeCollection.Name)]
public class UserProviderTests : UserProviderTestBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserProviderTests"/> class, targeting BTCUSDT under the
    /// configured <see cref="Settings.User"/> account.
    /// </summary>
    /// <param name="outputHelper">The xUnit output helper to route trace logging to.</param>
    public UserProviderTests(ITestOutputHelper outputHelper)
        : base(Settings.User, "BTCUSDT", outputHelper) { }

    /// <summary>
    /// Registers the Binance USD-M futures provider, with tight reload-loader intervals, so the user provider
    /// under test is resolved from its actual registration.
    /// </summary>
    /// <param name="ctx">The fluent context to register providers into.</param>
    protected override void RegisterProvider(ProviderRegistrationContext ctx)
    {
        ctx.WithBinanceUsdFutures(
            new ProviderConfiguration
            {
                ReloadContext = new CompositeLoaderConfig(200, 5, 1000, 1000, 100),
                ReloadOrders = new CompositeLoaderConfig(200, 5, 1000, 1000, 100),
                ReloadTrades = new CompositeLoaderConfig(200, 5, 1000, 1000, 100),
            }
        );
    }

    /// <summary>
    /// Loads the account's context from the live provider and asserts it reports assets. Talks to the real
    /// exchange; in the read block.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact(
        Skip = "needs exchange credentials in test.env",
        SkipUnless = nameof(Exchange.HasCredentials),
        SkipType = typeof(Exchange)
    )]
    public Task LoadContextAsync() => LoadContextBaseAsync();

    /// <summary>
    /// Loads the account's open orders from the live provider and asserts the call succeeds. Talks to the
    /// real exchange; in the read block.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact(
        Skip = "needs exchange credentials in test.env",
        SkipUnless = nameof(Exchange.HasCredentials),
        SkipType = typeof(Exchange)
    )]
    public Task LoadOpenOrdersAsync() => LoadOpenOrdersBaseAsync();

    /// <summary>
    /// Loads BTCUSDT's most recent orders (no time bound) from the live provider and asserts the call
    /// succeeds. Talks to the real exchange; in the read block.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact(
        Skip = "needs exchange credentials in test.env",
        SkipUnless = nameof(Exchange.HasCredentials),
        SkipType = typeof(Exchange)
    )]
    public Task LoadLatestOrdersAsync() => LoadLatestOrdersBaseAsync();

    /// <summary>
    /// Loads BTCUSDT's orders from the last day from the live provider and asserts the call succeeds. Talks
    /// to the real exchange; in the read block.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact(
        Skip = "needs exchange credentials in test.env",
        SkipUnless = nameof(Exchange.HasCredentials),
        SkipType = typeof(Exchange)
    )]
    public Task LoadHistoryOrdersAsync() => LoadHistoryOrdersBaseAsync();

    /// <summary>
    /// Loads BTCUSDT's most recent trades (no time bound) from the live provider and asserts the call
    /// succeeds. Talks to the real exchange; in the read block.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact(
        Skip = "needs exchange credentials in test.env",
        SkipUnless = nameof(Exchange.HasCredentials),
        SkipType = typeof(Exchange)
    )]
    public Task LoadLatestTradesAsync() => LoadLatestTradesBaseAsync();

    /// <summary>
    /// Loads BTCUSDT's trades from the last day from the live provider and asserts the call succeeds. Talks
    /// to the real exchange; in the read block.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact(
        Skip = "needs exchange credentials in test.env",
        SkipUnless = nameof(Exchange.HasCredentials),
        SkipType = typeof(Exchange)
    )]
    public Task LoadHistoryTradesAsync() => LoadHistoryTradesBaseAsync();
}
