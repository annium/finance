using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Connectors.Connectors;
using Annium.Finance.Providers.Shared.Connectors;
using Annium.Finance.Providers.Shared.ServerTime;
using Annium.Finance.Providers.Tests.Lib;
using Annium.Testing;
using Xunit;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Tests.Services;

public class ServerTimeProviderTests : ProvidersTestBase
{
    public ServerTimeProviderTests(ITestOutputHelper outputHelper)
        : base(ctx => ctx.WithBinanceUsdFutures(), outputHelper)
    {
        Inject(Settings.Market);
        Inject(Settings.User);
    }

    [Fact]
    public async Task Works()
    {
        // arrange
        var tracker = Get<IServerTimeTracker>();
        var monitor = Get<IStatusMonitor>();
        var status = ConnectorStatus.Disconnected;
        monitor.OnStatusChanged += s => status = s;

        // assert
        await Expect.ToAsync(() => status.Is(ConnectorStatus.Connected));
        tracker.ServerTime.IsNotDefault();
    }
}
