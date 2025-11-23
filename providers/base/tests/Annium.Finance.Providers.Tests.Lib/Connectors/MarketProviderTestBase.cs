using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Connectors.Connectors;
using Annium.Finance.Providers.Abstractions.Domain.Models;
using Annium.Finance.Providers.Abstractions.Domain.Operations;
using Annium.Finance.Providers.Shared;
using Annium.Logging;
using Annium.NodaTime.Extensions;
using Annium.Testing;
using NodaTime;
using Xunit;

namespace Annium.Finance.Providers.Tests.Lib.Connectors;

public abstract class MarketProviderTestBase : ProvidersTestBase
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
        var provider = GetKeyed<IMarketProvider>(providerKey.Provider);

        // act - load context
        var context = await provider.LoadContextAsync(providerKey.Environment);

        // assert - context
        context.Status.Is(MarketOperationStatus.Ok);
        var data = context.Data;
        data.IsNotDefault();
        data.Resources.Count.IsGreater(0);
        data.Instruments.Count.IsGreater(0);

        // act - load candles
        var end = SystemClock.Instance.GetCurrentInstant().FloorToMinute();
        var start = end - Duration.FromDays(2);
        var candles = new List<CandleModel>();
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
        firstCandle.Moment.Is(start.ToUnixTimeMilliseconds());
        firstCandle.Open.IsNotDefault();
        firstCandle.High.IsNotDefault();
        firstCandle.Low.IsNotDefault();
        firstCandle.Close.IsNotDefault();
        var lastCandle = candles[0];
        lastCandle.Moment.Is(start.ToUnixTimeMilliseconds());
        lastCandle.Open.IsNotDefault();
        lastCandle.High.IsNotDefault();
        lastCandle.Low.IsNotDefault();
        lastCandle.Close.IsNotDefault();

        this.Trace("done");
    }
}
