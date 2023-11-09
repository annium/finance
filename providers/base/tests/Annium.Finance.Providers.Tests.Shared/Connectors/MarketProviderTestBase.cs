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

        // act - load instruments
        this.Trace("load resources and instruments");
        var marketResult = await provider.LoadContextAsync(providerKey.Environment);
        marketResult.IsSuccess.IsTrue();
        var (resources, instruments) = marketResult.Data.NotNull();

        // assert - instruments
        instruments.Count.IsGreater(0);
        this.Trace<string>("resolve instrument for symbol {symbol}", _symbol);
        var instrument = instruments.Single(x => x.Symbol == _symbol);
        instrument.Target.IsNotDefault();
        resources.Contains(instrument.Target).IsTrue();
        instrument.Target.Code.IsNullOrWhiteSpace().IsFalse();
        instrument.Quote.IsNotDefault();
        resources.Contains(instrument.Quote).IsTrue();
        instrument.Quote.Code.IsNullOrWhiteSpace().IsFalse();
        instrument.Currency.IsNotDefault();
        resources.Contains(instrument.Currency).IsTrue();
        instrument.Currency.Code.IsNullOrWhiteSpace().IsFalse();
        instrument.Symbol.IsNullOrWhiteSpace().IsFalse();
        instrument.MinQty.IsNotDefault();
        instrument.MaxQty.IsNotDefault();
        instrument.LotSize.IsNotDefault();
        instrument.MinPrice.IsNotDefault();
        instrument.MaxPrice.IsNotDefault();
        instrument.TickSize.IsNotDefault();
        instrument.MinSum.IsNotDefault();
        instrument.MaxSum.IsNotDefault();
        instrument.MaxOrders.IsNotDefault();

        // act - load candles
        var end = SystemClock.Instance.GetCurrentInstant().FloorToMinute();
        var start = end - Duration.FromDays(10);
        var candles = new List<CandleDto>();
        this.Trace(
            "load candles in for {symbol} ({key}) in {start} - {end}",
            instrument.Symbol,
            providerKey,
            start,
            end
        );
        await foreach (
            var chunkResult in provider.LoadCandlesAsync(
                instrument.Symbol,
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
        candles.Count.Is(14400);
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
