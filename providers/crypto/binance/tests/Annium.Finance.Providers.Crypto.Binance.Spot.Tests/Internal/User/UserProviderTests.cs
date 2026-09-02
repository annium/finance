using System.Threading.Tasks;
using Annium.Finance.Providers.Core;
using Annium.Finance.Providers.Core.Shared.Loaders;
using Annium.Finance.Providers.Tests.Lib.User;
using Xunit;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Tests.Internal.User;

/// <summary>
/// Runs <see cref="UserProviderTestBase"/>'s context/orders/trades checks against the real Binance Spot user
/// provider for BTCUSDT. Currently disabled outright (not gated by
/// the read block like the USD-M futures counterpart):
/// every case is skipped with "Not implemented".
/// </summary>
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
    /// Registers the Binance Spot provider, with tight reload-loader intervals, so the user provider under
    /// test is resolved from its actual registration.
    /// </summary>
    /// <param name="ctx">The fluent context to register providers into.</param>
    protected override void RegisterProvider(ProviderRegistrationContext ctx)
    {
        ctx.WithBinanceSpot(
            new ProviderConfiguration
            {
                ReloadContext = new CompositeLoaderConfig(200, 5, 1000, 1000, 100),
                ReloadOrders = new CompositeLoaderConfig(200, 5, 1000, 1000, 100),
                ReloadTrades = new CompositeLoaderConfig(200, 5, 1000, 1000, 100),
            }
        );
    }

    /// <summary>Not implemented; skipped unconditionally.</summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact(Skip = "Not implemented")]
    public Task LoadContextAsync() => LoadContextBaseAsync();

    /// <summary>Not implemented; skipped unconditionally.</summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact(Skip = "Not implemented")]
    public Task LoadOpenOrdersAsync() => LoadOpenOrdersBaseAsync();

    /// <summary>Not implemented; skipped unconditionally.</summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact(Skip = "Not implemented")]
    public Task LoadLatestOrdersAsync() => LoadLatestOrdersBaseAsync();

    /// <summary>Not implemented; skipped unconditionally.</summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact(Skip = "Not implemented")]
    public Task LoadHistoryOrdersAsync() => LoadHistoryOrdersBaseAsync();

    /// <summary>Not implemented; skipped unconditionally.</summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact(Skip = "Not implemented")]
    public Task LoadLatestTradesAsync() => LoadLatestTradesBaseAsync();

    /// <summary>Not implemented; skipped unconditionally.</summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact(Skip = "Not implemented")]
    public Task LoadHistoryTradesAsync() => LoadHistoryTradesBaseAsync();
}
