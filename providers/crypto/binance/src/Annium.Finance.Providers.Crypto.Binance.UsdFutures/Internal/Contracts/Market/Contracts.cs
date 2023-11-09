using System.Text.Json;
using Annium.Core.DependencyInjection;
using Annium.Finance.Providers.Abstractions.Domain.Models;
using Annium.Finance.Providers.Crypto.Binance.Base.Contracts.Market.Converters;
using Annium.Finance.Providers.Crypto.Binance.Base.Contracts.Shared.Converters;
using Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Contracts.Market.Converters;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Contracts.Market;

internal class Contracts
{
    public JsonSerializerOptions ExchangeInfo { get; } =
        new JsonSerializerOptions()
            .ResetConverters()
            .AddConverter<ExchangeInfoConverter>()
            .AddConverter<RateLimitsConverter>()
            .AddConverter<InstrumentConverter>()
            .AddConverter<InstrumentFiltersConverter>();

    public JsonSerializerOptions InstrumentTicker { get; } =
        new JsonSerializerOptions()
            .ResetConverters()
            .AddConverter<InstrumentTickerConverter>()
            .AddConverter<StreamDataConverter<InstrumentTicker>>();

    public JsonSerializerOptions Candle { get; } =
        new JsonSerializerOptions().ResetConverters().AddConverter<CandleConverter>();
}
