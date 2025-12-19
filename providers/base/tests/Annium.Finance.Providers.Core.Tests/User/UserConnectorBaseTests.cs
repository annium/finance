using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Annium.Data.Tables;
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

public class UserConnectorBaseTests : ProvidersTestBase
{
    public UserConnectorBaseTests(ITestOutputHelper outputHelper)
        : base(outputHelper) { }

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
        var provider = new FakeUserProvider(Get<ITimeProvider>(), Logger);

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

    private FakeUserConnector CreateConnector(UserSettings settings, IUserProvider provider)
    {
        var reporter = Get<IStatusReporter>();
        var monitor = Get<IStatusMonitor>();

        return new FakeUserConnector(settings, provider, reporter, monitor, Logger);
    }

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

    private void VerifyLog(string type, TestLog<int> log)
    {
        this.Trace<string>("verify {type} log", type);
        var entries = log.ToArray();
        try
        {
            for (var i = 1; i < entries.Length - 1; i++)
                entries[i].Is(entries[i - 1] + 1);
        }
        catch
        {
            this.Error<string>("{type} log is not as expected:", type);
            for (var i = 0; i < entries.Length - 1; i++)
                this.Trace("{entry}", entries[i]);
            throw;
        }
    }

    private class FakeUserConnector : UserConnectorBase
    {
        public FakeUserConnector(
            UserSettings settings,
            IUserProvider userProvider,
            IStatusReporter reporter,
            IStatusMonitor monitor,
            ILogger logger
        )
            : base(settings, userProvider, reporter, monitor, logger) { }

        public void Asset(AssetModel x)
        {
            Write(ChangeEvent.Set(x));
        }

        public void Position(PositionModel x)
        {
            Write(ChangeEvent.Set(x));
        }

        public void Order(OrderModel x)
        {
            Write(ChangeEvent.Set(x));
        }

        public void Trade(TradeModel x)
        {
            Write(x);
        }
    }

    private class FakeUserProvider : UserProviderBase, IUserProvider
    {
        public FakeUserProvider(ITimeProvider timeProvider, ILogger logger)
            : base(timeProvider, logger) { }

        public Task<UserResult<UserContext?>> LoadContextAsync(UserSettings settings)
        {
            throw new NotImplementedException();
        }

        public Task<UserResult<IReadOnlyCollection<OrderModel>?>> LoadOpenOrdersAsync(UserSettings settings)
        {
            throw new NotImplementedException();
        }

        public Task<UserResult<IReadOnlyCollection<OrderModel>?>> LoadOrdersAsync(
            UserSettings settings,
            string symbol,
            long? since
        )
        {
            throw new NotImplementedException();
        }

        public Task<UserResult<IReadOnlyCollection<TradeModel>?>> LoadTradesAsync(
            UserSettings settings,
            string symbol,
            long? since
        )
        {
            throw new NotImplementedException();
        }
    }
}
