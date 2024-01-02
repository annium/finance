using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Connectors.Connectors;
using Annium.Finance.Providers.Shared.Connectors;
using Annium.Finance.Providers.Shared.ServerTime;
using Annium.Finance.Providers.Tests.Shared.Connectors;
using Annium.Finance.Providers.Tests.Shared.Extensions;
using Annium.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Tests.Services;

public class ServerTimeProviderTests : ConnectorTestBase
{
    public ServerTimeProviderTests(ITestOutputHelper outputHelper)
        : base(ctx => ctx.WithBinanceSpot(), outputHelper)
    {
        this.Inject(Markets.Test);
        this.Inject(Users.Test);
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
        await Expect.To(() => status.Is(ConnectorStatus.Connected));
        tracker.ServerTime.IsNotDefault();
    }
}
