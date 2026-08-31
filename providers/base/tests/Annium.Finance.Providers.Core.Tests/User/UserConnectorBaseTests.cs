using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Annium.Data.Tables;
using Annium.Finance.Providers.Abstractions.Connectors.Shared;
using Annium.Finance.Providers.Abstractions.Connectors.User;
using Annium.Finance.Providers.Abstractions.Domain.Shared;
using Annium.Finance.Providers.Abstractions.Domain.User;
using Annium.Finance.Providers.Abstractions.Domain.User.Operations;
using Annium.Finance.Providers.Core.Shared.Status;
using Annium.Finance.Providers.Core.User;
using Annium.Finance.Providers.Tests.Lib;
using Annium.Logging;
using Annium.Testing;
using Annium.Threading.Tasks;
using Xunit;

namespace Annium.Finance.Providers.Core.Tests.User;

/// <summary>
/// Pins the sync lifecycle of <see cref="UserConnectorBase"/>: that a sync call runs the connector's own
/// <see cref="UserConnectorBase.OnSync"/> handler, and that assets, positions, orders and trades written while
/// resubscription is in flight are still delivered to subscribers in order.
/// </summary>
public class UserConnectorBaseTests : ProvidersTestBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserConnectorBaseTests"/> class.
    /// </summary>
    /// <param name="outputHelper">The xUnit output helper used to capture test logs.</param>
    public UserConnectorBaseTests(ITestOutputHelper outputHelper)
        : base(outputHelper) { }

    /// <summary>
    /// Verifies that triggering a sync on a connector invokes <see cref="UserConnectorBase.OnSync"/> with the
    /// settings and provider it was constructed with, and still delivers every asset, position, order and trade
    /// emitted during the sync to subscribers, in order.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task Sync_Works()
    {
        // arrange
        // prerequisites
        this.Trace("prepare components");
        var settings = new UserSettings
        {
            Provider = "fake",
            Environment = ProviderEnvironment.Test,
            Key = "some_key",
            Secret = "some_secret",
        };
        var provider = new FakeUserProvider();

        // data
        this.Trace("prepare data");
        const int dataSize = 1000;
        var assets = Enumerable.Range(0, dataSize).Select(i => new AssetModel(i.ToString(), 0m, 0m)).ToArray();
        var positions = Enumerable
            .Range(0, dataSize)
            .Select(i => new PositionModel(i.ToString(), OrientationRange.Both, MarginType.Cross, 0m, 0m))
            .ToArray();
        var orders = Enumerable
            .Range(0, dataSize)
            .Select(i => new OrderModel(
                i.ToString(),
                string.Empty,
                OrientationRange.Both,
                string.Empty,
                OrderSide.Buy,
                OrderType.Limit,
                1m,
                1m,
                0m,
                false,
                0,
                OrderStatus.New,
                0m,
                0m,
                0
            ))
            .ToArray();
        var trades = Enumerable
            .Range(0, dataSize)
            .Select(i => new TradeModel(i.ToString(), string.Empty, string.Empty, 0m, 0m, string.Empty, 0m, true, 0L))
            .ToArray();

        // act

        this.Trace("create connector");
        await using var user = CreateConnector(settings, provider);

        this.Trace("setup sync handler");
        user.OnSync += async (s, p) =>
        {
            s.Is(settings);
            p.Is(provider);

            this.Trace("sync:start");
            await Task.Delay(200);
            this.Trace("sync:done");
        };

        this.Trace("subscribe to user data");
        var assetsLog = new TestLog<int>();
        user.Assets.Subscribe(e => assetsLog.Add(int.Parse(e.Item.Resource)));
        var positionsLog = new TestLog<int>();
        user.Positions.Subscribe(e => positionsLog.Add(int.Parse(e.Item.Symbol)));
        var ordersLog = new TestLog<int>();
        user.Orders.Subscribe(e => ordersLog.Add(int.Parse(e.Item.Id)));
        var tradesLog = new TestLog<int>();
        user.Trades.Subscribe(e => tradesLog.Add(int.Parse(e.Id)));

        this.Trace("run assets emit");
        Emit(assets, user.Asset);

        this.Trace("run positions emit");
        Emit(positions, user.Position);

        this.Trace("run orders emit");
        Emit(orders, user.Order);

        this.Trace("run trades emit");
        Emit(trades, user.Trade);

        this.Trace("trigger sync");
        user.Sync();

        // assert (data messages)
        this.Trace("await for all events");
        await Wait.UntilAsync(() =>
            assetsLog.Count == dataSize
            && positionsLog.Count == dataSize
            && ordersLog.Count == dataSize
            && tradesLog.Count == dataSize
        );

        this.Trace("examine event logs");
        VerifyLog("assets", assetsLog);
        VerifyLog("positions", positionsLog);
        VerifyLog("orders", ordersLog);
        VerifyLog("trades", tradesLog);
    }

    /// <summary>
    /// Verifies that a sync handler which throws is surfaced through <see cref="Abstractions.Connectors.Shared.IConnectorBase.OnError"/>
    /// and does not leave the connector claiming to be connected. The cycle unsubscribes its readers before
    /// calling the handler, so a throw that goes unreported strands the connector with no subscriptions, no
    /// status change, and nothing said to the caller.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task SyncHandlerThrows_IsReportedAndNotClaimedConnected()
    {
        // arrange
        var settings = new UserSettings
        {
            Provider = "fake",
            Environment = ProviderEnvironment.Test,
            Key = "some_key",
            Secret = "some_secret",
        };
        await using var user = CreateConnector(settings, new FakeUserProvider());
        var errors = new ConcurrentQueue<ConnectorError>();
        var statuses = new ConcurrentQueue<ConnectorStatus>();
        user.OnError += errors.Enqueue;
        user.OnStatusChanged += statuses.Enqueue;

        user.OnSync += (_, _) => throw new InvalidOperationException("sync failed");

        // act
        user.Sync();

        // assert - the failure reaches the caller, and the connector does not call itself connected
        await Expect.ToAsync(() => errors.Count.IsGreaterOrEqual(1));
        statuses.Contains(ConnectorStatus.Connected).IsFalse("a failed sync must not report connected");
    }

    /// <summary>
    /// An error a component reports through its status reporter reaches the connector's own listeners — the
    /// far half of the relay whose near end the campaign already repaired in the status monitor. The other
    /// route into <c>OnError</c>, a sync handler that throws, is tested above and does not touch this one.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ErrorReportedByAnotherComponent_ReachesTheConnector()
    {
        // arrange - a second component bound to the same monitor, as a provider's services are
        var other = Get<IStatusReporter>();
        other.Bind("other", ConnectorStatus.Connected);

        var settings = new UserSettings
        {
            Provider = "fake",
            Environment = ProviderEnvironment.Test,
            Key = "some_key",
            Secret = "some_secret",
        };
        await using var user = CreateConnector(settings, new FakeUserProvider());
        var errors = new ConcurrentQueue<ConnectorError>();
        user.OnError += errors.Enqueue;

        // act
        other.Error(new ConnectorError("listen key expired"));

        // assert
        await Expect.ToAsync(() => errors.Count.Is(1));
        errors.TryPeek(out var error).IsTrue();
        error.NotNull().Message.Is("listen key expired", "the error must arrive intact, not merely as a signal");
    }

    /// <summary>
    /// Builds a <see cref="FakeUserConnector"/> wired to the given provider and this test's status reporter and
    /// monitor.
    /// </summary>
    /// <param name="settings">The user settings to construct the connector with.</param>
    /// <param name="provider">The user provider backing the connector.</param>
    /// <returns>The constructed connector.</returns>
    private FakeUserConnector CreateConnector(UserSettings settings, IUserProvider provider)
    {
        var reporter = Get<IStatusReporter>();
        var monitor = Get<IStatusMonitor>();

        return new FakeUserConnector(settings, provider, reporter, monitor, Logger);
    }

    /// <summary>
    /// Schedules a background task that feeds each item in <paramref name="data"/> into <paramref name="emit"/>
    /// with a short delay between items, so items arrive while the connector's sync is still in flight.
    /// </summary>
    /// <typeparam name="T">The type of item emitted.</typeparam>
    /// <param name="data">The items to emit, in order.</param>
    /// <param name="emit">The callback that pushes an item into the connector under test.</param>
    private void Emit<T>(IReadOnlyList<T> data, Action<T> emit)
    {
        Task.Run(
                async () =>
                {
                    await Task.Delay(10);
                    foreach (var x in data)
                    {
                        await Task.Delay(1);
                        emit(x);
                    }
                },
                TestContext.Current.CancellationToken
            )
            .GetAwaiter();
    }

    /// <summary>
    /// Asserts that the recorded values in <paramref name="log"/> form a contiguous increasing sequence, proving
    /// none were dropped or reordered; logs the captured entries and rethrows on failure.
    /// </summary>
    /// <param name="type">The label identifying which log is being verified, used in diagnostics.</param>
    /// <param name="log">The log of observed values to verify.</param>
    private void VerifyLog(string type, TestLog<int> log)
    {
        this.Trace<string>("verify {type} log", type);
        var entries = log.ToArray();
        try
        {
            // to entries.Length, not one short of it: stopping early left the final value asserted
            // by nothing. The count is gated separately, so a plain drop was caught - but a last
            // entry that arrived duplicated or out of order passed, and order is what this proves
            for (var i = 1; i < entries.Length; i++)
                entries[i].Is(entries[i - 1] + 1);
        }
        catch
        {
            this.Error<string>("{type} log is not as expected:", type);
            for (var i = 0; i < entries.Length; i++)
                this.Trace("{entry}", entries[i]);
            throw;
        }
    }

    /// <summary>
    /// Exposes <see cref="UserConnectorBase"/>'s protected write operations as public methods, so the test can
    /// drive them directly without a real user provider.
    /// </summary>
    private class FakeUserConnector : UserConnectorBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FakeUserConnector"/> class.
        /// </summary>
        /// <param name="settings">The user settings to construct the connector with.</param>
        /// <param name="provider">The user provider backing the connector.</param>
        /// <param name="reporter">The status reporter to bind to.</param>
        /// <param name="monitor">The status monitor to observe.</param>
        /// <param name="logger">The logger to use.</param>
        public FakeUserConnector(
            UserSettings settings,
            IUserProvider provider,
            IStatusReporter reporter,
            IStatusMonitor monitor,
            ILogger logger
        )
            : base(settings, provider, reporter, monitor, Annium.Disposable.AsyncBox(logger), logger) { }

        /// <summary>Writes an asset upsert to the connector's output, exposing the protected <see cref="UserConnectorBase.Write(ChangeEvent{AssetModel})"/> call.</summary>
        /// <param name="x">The asset to write.</param>
        public void Asset(AssetModel x)
        {
            Write(ChangeEvent.Set(x));
        }

        /// <summary>Writes a position upsert to the connector's output, exposing the protected <see cref="UserConnectorBase.Write(ChangeEvent{PositionModel})"/> call.</summary>
        /// <param name="x">The position to write.</param>
        public void Position(PositionModel x)
        {
            Write(ChangeEvent.Set(x));
        }

        /// <summary>Writes an order upsert to the connector's output, exposing the protected <see cref="UserConnectorBase.Write(ChangeEvent{OrderModel})"/> call.</summary>
        /// <param name="x">The order to write.</param>
        public void Order(OrderModel x)
        {
            Write(ChangeEvent.Set(x));
        }

        /// <summary>Writes a trade to the connector's output, exposing the protected <see cref="UserConnectorBase.Write(TradeModel)"/> call.</summary>
        /// <param name="x">The trade to write.</param>
        public void Trade(TradeModel x)
        {
            Write(x);
        }
    }

    /// <summary>
    /// Stands in for a real <see cref="IUserProvider"/>; unlike a real provider, every member throws because the
    /// test never calls them - only <see cref="FakeUserConnector"/>'s exposed write operations are exercised.
    /// </summary>
    private class FakeUserProvider : IUserProvider
    {
        /// <summary>Not implemented; not exercised by these tests.</summary>
        /// <returns>Never returns.</returns>
        public Task<UserResult<UserContext?>> LoadContextAsync()
        {
            throw new NotImplementedException();
        }

        /// <summary>Not implemented; not exercised by these tests.</summary>
        /// <returns>Never returns.</returns>
        public Task<UserResult<IReadOnlyCollection<OrderModel>?>> LoadOpenOrdersAsync()
        {
            throw new NotImplementedException();
        }

        /// <summary>Not implemented; not exercised by these tests.</summary>
        /// <param name="symbol">Unused.</param>
        /// <param name="since">Unused.</param>
        /// <returns>Never returns.</returns>
        public Task<UserResult<IReadOnlyCollection<OrderModel>?>> LoadOrdersAsync(string symbol, long? since)
        {
            throw new NotImplementedException();
        }

        /// <summary>Not implemented; not exercised by these tests.</summary>
        /// <param name="symbol">Unused.</param>
        /// <param name="since">Unused.</param>
        /// <returns>Never returns.</returns>
        public Task<UserResult<IReadOnlyCollection<TradeModel>?>> LoadTradesAsync(string symbol, long? since)
        {
            throw new NotImplementedException();
        }
    }
}
