using System.Text.Json;
using Annium.Finance.Providers.Abstractions.Domain.Market;
using Annium.Finance.Providers.Crypto.Binance.Base.Market.Contracts.Converters;
using Annium.Finance.Providers.Crypto.Binance.Base.Shared.Contracts.Converters;
using Annium.Finance.Providers.Crypto.Binance.Spot.Internal.Market.Contracts.Converters;
using Annium.Serialization.Json;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Internal.Market.Contracts;

/// <summary>Preconfigured JSON serializer options for each Binance spot market-data endpoint.</summary>
internal static class MarketContracts
{
    /// <summary>Serializer options for the exchange info (instruments and rate limits) endpoint.</summary>
    public static JsonSerializerOptions ExchangeInfo { get; } =
        new JsonSerializerOptions()
            .ResetConverters()
            .AddConverter<ExchangeInfoConverter>()
            .AddConverter<RateLimitsConverter>()
            .AddConverter<InstrumentConverter>()
            .AddConverter<InstrumentFiltersConverter>()
            .AddConverter<OperationResultConverter>();

    /// <summary>Serializer options for the instrument ticker stream endpoint.</summary>
    public static JsonSerializerOptions InstrumentTicker { get; } =
        new JsonSerializerOptions()
            .ResetConverters()
            .AddConverter<CommandResultConverter>()
            .AddConverter<InstrumentTickerConverter>()
            .AddConverter<StreamDataConverter<InstrumentTicker>>()
            .AddConverter<OperationResultConverter>();

    /// <summary>Serializer options for the candle history endpoint.</summary>
    public static JsonSerializerOptions Candle { get; } =
        new JsonSerializerOptions()
            .ResetConverters()
            .AddConverter<CandleConverter>()
            .AddConverter<OperationResultConverter>();
}
