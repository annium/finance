namespace Annium.Finance.Providers.Abstractions.Domain.Market;

/// <summary>
/// Represents a single OHLCV candle for an instrument over one time interval.
/// </summary>
/// <param name="Moment">The Unix timestamp, in milliseconds, marking the start of the candle's interval.</param>
/// <param name="Open">The price at the start of the interval.</param>
/// <param name="High">The highest price reached during the interval.</param>
/// <param name="Low">The lowest price reached during the interval.</param>
/// <param name="Close">The price at the end of the interval.</param>
/// <param name="Volume">The total traded volume during the interval, in the instrument's base asset.</param>
public sealed record CandleModel(long Moment, decimal Open, decimal High, decimal Low, decimal Close, decimal Volume);
