using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Annium.Core.DependencyInjection;
using Annium.Finance.Providers.Abstractions.Connectors.Connectors;
using Annium.Finance.Providers.Abstractions.Domain.Dto;
using Annium.Finance.Providers.Abstractions.Domain.Models;
using Annium.Finance.Providers.Shared;
using Annium.Logging;
using Annium.NodaTime.Extensions;
using Annium.Testing;
using NodaTime;
using Xunit.Abstractions;

namespace Annium.Finance.Providers.Tests.Shared.Connectors;

public abstract class MarketProviderTestBase : ConnectorTestBase
{
    private readonly string _symbol;

    protected MarketProviderTestBase(
        Action<ProviderRegistrationContext> registerProvider,
        string symbol,
        ITestOutputHelper outputHelper
    )
        : base(registerProvider, outputHelper)
    {
        _symbol = symbol;
    }

    protected async Task MarketProviderBaseAsync(ProviderKey providerKey)
    {
        this.Trace("start");

        // arrange
        var keys = Get<IEnumerable<ProviderKey>>().ToArray();
        keys.Contains(providerKey).IsTrue();

        // act - resolve market provider
        this.Trace("resolve market provider");
        var provider = Get<IIndex<string, IMarketProvider>>()[providerKey.Provider];

        // act - load candles
        var end = SystemClock.Instance.GetCurrentInstant().FloorToMinute();
        var start = end - Duration.FromDays(2);
        var candles = new List<CandleDto>();
        this.Trace("load candles in for {symbol} ({key}) in {start} - {end}", _symbol, providerKey, start, end);
        await foreach (
            var chunkResult in provider.LoadCandlesAsync(
                _symbol,
                providerKey.Environment,
                start,
                end,
                CancellationToken.None
            )
        )
        {
            chunkResult.IsSuccess.IsTrue();
            candles.AddRange(chunkResult.Data.NotNull());
        }

        // assert - candles
        this.Trace("verify candles");
        candles.Count.Is(2880);
        var firstCandle = candles[0];
        firstCandle.Moment.Is(start);
        firstCandle.Open.IsNotDefault();
        firstCandle.High.IsNotDefault();
        firstCandle.Low.IsNotDefault();
        firstCandle.Close.IsNotDefault();
        var lastCandle = candles[0];
        lastCandle.Moment.Is(start);
        lastCandle.Open.IsNotDefault();
        lastCandle.High.IsNotDefault();
        lastCandle.Low.IsNotDefault();
        lastCandle.Close.IsNotDefault();

        this.Trace("done");
    }
}
