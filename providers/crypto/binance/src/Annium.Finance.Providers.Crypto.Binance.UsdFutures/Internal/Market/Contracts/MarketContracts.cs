using System.Text.Json;
using Annium.Finance.Providers.Abstractions.Domain.Market;
using Annium.Finance.Providers.Crypto.Binance.Base.Market.Contracts.Converters;
using Annium.Finance.Providers.Crypto.Binance.Base.Shared.Contracts.Converters;
using Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Market.Contracts.Converters;
using Annium.Serialization.Json;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Market.Contracts;

/// <summary>
/// Per-endpoint JSON serializer options for the USD-M futures market data endpoints, each wiring the converters
/// needed to parse that endpoint's response shape.
/// </summary>
internal static class MarketContracts
{
    /// <summary>Serializer options for the <c>GET /fapi/v1/exchangeInfo</c> endpoint.</summary>
    public static JsonSerializerOptions ExchangeInfo { get; } =
        new JsonSerializerOptions()
            .ResetConverters()
            .AddConverter<ExchangeInfoConverter>()
            .AddConverter<RateLimitsConverter>()
            .AddConverter<AssetConverter>()
            .AddConverter<InstrumentConverter>()
            .AddConverter<InstrumentFiltersConverter>()
            .AddConverter<OperationResultConverter>();

    /// <summary>Serializer options for the book ticker (best bid/ask) websocket stream.</summary>
    public static JsonSerializerOptions InstrumentTicker { get; } =
        new JsonSerializerOptions()
            .ResetConverters()
            .AddConverter<CommandResultConverter>()
            .AddConverter<InstrumentTickerConverter>()
            .AddConverter<StreamDataConverter<InstrumentTicker>>()
            .AddConverter<OperationResultConverter>();

    /// <summary>Serializer options for the historical klines/candles endpoint.</summary>
    public static JsonSerializerOptions Candle { get; } =
        new JsonSerializerOptions()
            .ResetConverters()
            .AddConverter<CandleConverter>()
            .AddConverter<OperationResultConverter>();
}
