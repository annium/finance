using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Annium.Data.Tables;
using Annium.Finance.Providers.Abstractions.Connectors.Connectors;
using Annium.Finance.Providers.Abstractions.Domain.Enums;
using Annium.Finance.Providers.Abstractions.Domain.Models;
using Annium.Finance.Providers.Abstractions.Domain.Operations;
using Annium.Finance.Providers.Shared.Connectors;
using Annium.Finance.Providers.Tests.Lib;
using Annium.Logging;
using Annium.Testing;
using Annium.Threading.Channels;
using Annium.Threading.Tasks;
using Xunit;

namespace Annium.Finance.Providers.Shared.Tests.Connectors;

public class UserConnectorBaseTests : ProvidersTestBase
{
    public UserConnectorBaseTests(ITestOutputHelper outputHelper)
        : base(outputHelper) { }

    [Fact]
    public async Task Sync_Works()
    {
        // arrange
        // prerequisites
        var provider = new FakeUserProvider(Get<ITimeProvider>(), Logger);
        var monitor = Get<IStatusMonitor>();
        var reporter = Get<IStatusReporter>();
        reporter.Bind(this);
        var settings = new UserSettings
        {
            Provider = "fake",
            Environment = ProviderEnvironment.Test,
            Key = "some_key",
            Secret = "some_secret",
        };

        // data
        var count = 1000;
        var assets = Enumerable.Range(0, count).Select(i => new AssetModel(i.ToString(), 0m, 0m)).ToArray();
        var positions = Enumerable
            .Range(0, count)
            .Select(i => new PositionModel(i.ToString(), OrientationRange.Both, MarginType.Cross, 0m, 0m))
            .ToArray();
        var orders = Enumerable
            .Range(0, count)
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
            .Range(0, count)
            .Select(i => new TradeModel(i.ToString(), string.Empty, string.Empty, 0m, 0m, string.Empty, 0m, true, 0L))
            .ToArray();

        var log = new TestLog<string>();

        await using var user = new FakeUserConnector(settings, provider, monitor, Logger);
        user.OnSync += async (s, p) =>
        {
            s.Is(settings);
            p.Is(provider);

            log.Add("sync:start");
            await Task.Delay(200);
            log.Add("sync:done");
        };
        user.Assets.Subscribe(e => log.Add($"A:{e.Item.Resource}"));
        user.Positions.Subscribe(e => log.Add($"P:{e.Item.Symbol}"));
        user.Orders.Subscribe(e => log.Add($"O:{e.Item.Id}"));
        user.Trades.Subscribe(e => log.Add($"T:{e.Id}"));
        Task.Run(
                async () =>
                {
                    await Task.Delay(10);
                    foreach (var x in assets)
                    {
                        await Task.Delay(1);
                        user.Asset(x);
                    }
                },
                TestContext.Current.CancellationToken
            )
            .GetAwaiter();
        Task.Run(
                async () =>
                {
                    await Task.Delay(10);
                    foreach (var x in positions)
                    {
                        await Task.Delay(1);
                        user.Position(x);
                    }
                },
                TestContext.Current.CancellationToken
            )
            .GetAwaiter();
        Task.Run(
                async () =>
                {
                    await Task.Delay(10);
                    foreach (var x in orders)
                    {
                        await Task.Delay(1);
                        user.Order(x);
                    }
                },
                TestContext.Current.CancellationToken
            )
            .GetAwaiter();
        Task.Run(
                async () =>
                {
                    await Task.Delay(10);
                    foreach (var x in trades)
                    {
                        await Task.Delay(1);
                        user.Trade(x);
                    }
                },
                TestContext.Current.CancellationToken
            )
            .GetAwaiter();
        Task.Run(
                async () =>
                {
                    await Task.Delay(10);
                    reporter.Connected();
                    await Task.Delay(10);
                    reporter.Connecting();
                    await Task.Delay(10);
                    reporter.Connected();
                },
                TestContext.Current.CancellationToken
            )
            .GetAwaiter();

        // assert (data messages + 2 sync event pairs)
        var targetCount = count * 4 + 4;
        await Wait.UntilAsync(() => log.Count == targetCount);
        var entries = log.ToArray();
        try
        {
            for (var i = 0; i < entries.Length - 1; i++)
            {
                if (entries[i] == "sync:start")
                    entries[i + 1].Is("sync:done");
            }
        }
        catch
        {
            this.Trace("entries");
            for (var i = 0; i < entries.Length - 1; i++)
                this.Trace(entries[i]);
            throw;
        }
    }

    private class FakeUserConnector : UserConnectorBase
    {
        public FakeUserConnector(
            UserSettings settings,
            IUserProvider userProvider,
            IStatusMonitor monitor,
            ILogger logger
        )
            : base(settings, userProvider, monitor, logger) { }

        public void Asset(AssetModel x)
        {
            AssetWriter.Write(ChangeEvent.Set(x));
        }

        public void Position(PositionModel x)
        {
            PositionWriter.Write(ChangeEvent.Set(x));
        }

        public void Order(OrderModel x)
        {
            OrderWriter.Write(ChangeEvent.Set(x));
        }

        public void Trade(TradeModel x)
        {
            TradeWriter.Write(x);
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
