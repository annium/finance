namespace Annium.Finance.Providers.Abstractions.Domain.Market;

/// <summary>
/// Represents the trading constraints of an instrument: the quantity and price steps and bounds orders must satisfy.
/// </summary>
public interface IInstrument
{
    /// <summary>Gets the quantity step orders must be a multiple of, in the instrument's base asset.</summary>
    decimal LotSize { get; }

    /// <summary>Gets the price step order prices must be a multiple of.</summary>
    decimal TickSize { get; }

    /// <summary>Gets the minimum order quantity, in the instrument's base asset.</summary>
    decimal MinQty { get; }

    /// <summary>Gets the maximum order quantity, in the instrument's base asset.</summary>
    decimal MaxQty { get; }

    /// <summary>Gets the minimum allowed order price.</summary>
    decimal MinPrice { get; }

    /// <summary>Gets the maximum allowed order price.</summary>
    decimal MaxPrice { get; }

    /// <summary>Gets the minimum order notional value (quantity multiplied by price).</summary>
    decimal MinSum { get; }

    /// <summary>Gets the maximum order notional value (quantity multiplied by price).</summary>
    decimal MaxSum { get; }
}
