using System.Text.Json;
using Annium.Finance.Providers.Abstractions.Domain.Market;
using Annium.Finance.Providers.Crypto.Binance.Base.Market.Contracts.Converters;
using Annium.Finance.Providers.Crypto.Binance.Base.Shared.Contracts.Converters;
using Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Market.Contracts.Converters;
using Annium.Serialization.Json;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Market.Contracts;

internal static class MarketContracts
{
    public static JsonSerializerOptions ExchangeInfo { get; } =
        new JsonSerializerOptions()
            .ResetConverters()
            .AddConverter<ExchangeInfoConverter>()
            .AddConverter<RateLimitsConverter>()
            .AddConverter<AssetConverter>()
            .AddConverter<InstrumentConverter>()
            .AddConverter<InstrumentFiltersConverter>()
            .AddConverter<OperationResultConverter>();

    public static JsonSerializerOptions InstrumentTicker { get; } =
        new JsonSerializerOptions()
            .ResetConverters()
            .AddConverter<CommandResultConverter>()
            .AddConverter<InstrumentTickerConverter>()
            .AddConverter<StreamDataConverter<InstrumentTicker>>()
            .AddConverter<OperationResultConverter>();

    public static JsonSerializerOptions Candle { get; } =
        new JsonSerializerOptions()
            .ResetConverters()
            .AddConverter<CandleConverter>()
            .AddConverter<OperationResultConverter>();
}
