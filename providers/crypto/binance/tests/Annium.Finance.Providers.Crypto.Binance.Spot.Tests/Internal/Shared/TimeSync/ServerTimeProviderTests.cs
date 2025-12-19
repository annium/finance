using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Connectors.Shared;
using Annium.Finance.Providers.Core;
using Annium.Finance.Providers.Core.Shared.Status;
using Annium.Finance.Providers.Core.Shared.TimeSync;
using Annium.Finance.Providers.Tests.Lib;
using Annium.Testing;
using Xunit;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Tests.Internal.Shared.TimeSync;

public class ServerTimeProviderTests : ProvidersTestBase
{
    public ServerTimeProviderTests(ITestOutputHelper outputHelper)
        : base(outputHelper)
    {
        this.Inject(Settings.Market);
        this.Inject(Settings.User);
    }

    protected override void RegisterProvider(ProviderRegistrationContext ctx)
    {
        ctx.WithBinanceSpot();
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
