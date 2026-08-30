using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Connectors.Shared;
using Annium.Finance.Providers.Abstractions.Domain.Shared;
using Annium.Finance.Providers.Core;
using Annium.Finance.Providers.Core.Shared.Status;
using Annium.Finance.Providers.Core.Shared.TimeSync;
using Annium.Finance.Providers.Tests.Lib;
using Annium.Testing;
using Xunit;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Tests.Internal.Shared.TimeSync;

/// <summary>
/// Verifies that the Spot <see cref="IServerTimeSource"/> connects to the live exchange, reports
/// <see cref="ConnectorStatus.Connected"/>, and starts tracking a synced server time. Read-only, but it does
/// open a real connection to Binance, so it runs only when <see cref="Exchange.IsEnabled"/> is set.
/// </summary>
public class ServerTimeProviderTests : ProvidersTestBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ServerTimeProviderTests"/> class.
    /// </summary>
    /// <param name="outputHelper">The xUnit output helper to route trace logging to.</param>
    public ServerTimeProviderTests(ITestOutputHelper outputHelper)
        : base(outputHelper) { }

    /// <summary>
    /// Registers the Binance Spot provider so the time source under test is resolved from its actual registration.
    /// </summary>
    /// <param name="ctx">The fluent context to register providers into.</param>
    protected override void RegisterProvider(ProviderRegistrationContext ctx)
    {
        ctx.WithBinanceSpot();
    }

    /// <summary>
    /// Waits for the connector status to reach <see cref="ConnectorStatus.Connected"/> against the live
    /// exchange, then asserts the server time source has picked up a value. Talks to the real exchange;
    /// skipped unless <see cref="Exchange.IsEnabled"/> is set.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact(Skip = "talks to the live exchange", SkipUnless = nameof(Exchange.IsEnabled), SkipType = typeof(Exchange))]
    public async Task Works()
    {
        // arrange
        var source = GetKeyed<IServerTimeSource>(Settings.Market.GetProviderKey());
        var monitor = Get<IStatusMonitor>();
        var status = ConnectorStatus.Disconnected;
        monitor.OnStatusChanged += s => status = s;

        // assert
        await Expect.ToAsync(() => status.Is(ConnectorStatus.Connected));
        source.ServerTime.IsNotDefault();
    }
}
