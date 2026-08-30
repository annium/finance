using Annium.Finance.Providers.Abstractions.Domain.Market;
using Annium.Finance.Providers.Abstractions.Domain.Shared;
using Annium.Finance.Providers.Tests.Lib.Market;
using Annium.Testing;
using Xunit;

namespace Annium.Finance.Providers.Abstractions.Domain.Tests.Market;

/// <summary>
/// Pins the rounding, clamping and validity rules <see cref="InstrumentExtensions"/> derives from an
/// <see cref="IInstrument"/>'s lot size, tick size and quantity/notional bounds.
/// </summary>
public class InstrumentExtensionsTests
{
    /// <summary>An instrument with default tick and lot sizes, used by every test that doesn't need bespoke bounds.</summary>
    private readonly IInstrument _instrument = InstrumentHelper.DefaultInstrument;

    /// <summary>
    /// Verifies that <see cref="InstrumentExtensions.TickPrecision{TInstrument}"/> counts the decimal digits of the
    /// tick size, independent of the price's own precision.
    /// </summary>
    [Fact]
    public void TickPrecision()
    {
        // assert
        InstrumentHelper.CreateInstrument("x", "y", 100m, 10m).TickPrecision().Is(0);
        InstrumentHelper.CreateInstrument("x", "y", 75m, 7.50m).TickPrecision().Is(1);
        InstrumentHelper.CreateInstrument("x", "y", 7.5m, 0.75m).TickPrecision().Is(2);
        InstrumentHelper.CreateInstrument("x", "y", 1m, 0.01m).TickPrecision().Is(2);
    }

    /// <summary>
    /// Verifies that <see cref="InstrumentExtensions.ToValidQty{TInstrument}"/> rounds a quantity to the lot size
    /// and then clamps it to the instrument's minimum and maximum quantity.
    /// </summary>
    [Fact]
    public void ToValidQty()
    {
        // assert
        _instrument.ToValidQty(1.56m).Is(1.5m);
        _instrument.ToValidQty(0.05m).Is(_instrument.MinQty);
        _instrument.ToValidQty(105m).Is(_instrument.MaxQty);
    }

    /// <summary>
    /// Verifies that <see cref="InstrumentExtensions.IsValidQtyPrice{TInstrument}"/> rejects a quantity or price
    /// that isn't already aligned, and a notional value under the minimum sum, while accepting values that
    /// satisfy every constraint at once.
    /// </summary>
    [Fact]
    public void IsValidQtyPrice()
    {
        // assert
        // invalid qty
        _instrument.IsValidQtyPrice(2.01m, 0.1m).IsFalse();
        // invalid price
        _instrument.IsValidQtyPrice(2m, 0.101m).IsFalse();
        // less than min sum
        _instrument.IsValidQtyPrice(2m, 0.1m).IsFalse();
        // valid
        _instrument.IsValidQtyPrice(2m, 0.5m).IsTrue();
    }

    /// <summary>Verifies that <see cref="InstrumentExtensions.ToTickSizeDown{TInstrument}"/> always rounds a price down to the nearest tick.</summary>
    [Fact]
    public void ToTickSizeDown()
    {
        // assert
        _instrument.ToTickSizeDown(0.126m).Is(0.12m);
    }

    /// <summary>Verifies that <see cref="InstrumentExtensions.ToTickSizeRound{TInstrument}"/> rounds a price to the nearest tick, up or down depending on which is closer.</summary>
    [Fact]
    public void ToTickSizeRound()
    {
        // arrange
        _instrument.ToTickSizeRound(0.124m).Is(0.12m);
        _instrument.ToTickSizeRound(0.126m).Is(0.13m);
    }

    /// <summary>Verifies that <see cref="InstrumentExtensions.ToTickSizeUp{TInstrument}"/> always rounds a price up to the nearest tick.</summary>
    [Fact]
    public void ToTickSizeUp()
    {
        // arrange
        _instrument.ToTickSizeUp(0.124m).Is(0.13m);
    }

    /// <summary>Verifies that <see cref="InstrumentExtensions.ToLotSize{TInstrument}"/> rounds a quantity down to the nearest lot size.</summary>
    [Fact]
    public void ToLotSize()
    {
        // arrange
        _instrument.ToLotSize(0.12m).Is(0.1m);
    }

    /// <summary>
    /// Verifies that a negative quantity - the way a sell or short size is expressed - is rounded towards
    /// zero rather than down, so aligning it can only shrink the order, never grow it.
    /// </summary>
    [Fact]
    public void ToLotSize_NegativeQty_RoundsTowardsZero()
    {
        // assert
        _instrument.ToLotSize(-0.12m).Is(-0.1m, "aligning a sell size must not ask for more than was requested");
        _instrument.ToLotSize(-1.56m).Is(-1.5m);
        _instrument.ToValidQty(-105m).Is(-_instrument.MaxQty);
    }

    /// <summary>
    /// Verifies that an instrument reporting no lot or tick step leaves quantities and prices untouched
    /// instead of dividing by zero.
    /// </summary>
    [Fact]
    public void ZeroLotAndTick_LeaveValuesUntouched()
    {
        // arrange
        var instrument = InstrumentHelper.CreateInstrument("x", "y", 0m, 0m);

        // assert
        instrument.ToLotSize(0.123m).Is(0.123m);
        instrument.ToTickSizeDown(0.123m).Is(0.123m);
        instrument.ToTickSizeRound(0.123m).Is(0.123m);
        instrument.ToTickSizeUp(0.123m).Is(0.123m);
    }

    /// <summary>
    /// Verifies that a price outside the instrument's own price bounds is rejected even when it is tick
    /// aligned and its notional value is in range - the constraint the exchange reports and would enforce.
    /// </summary>
    [Fact]
    public void IsValidQtyPrice_PriceOutOfBounds_IsRejected()
    {
        // arrange - prices allowed in [10, 100], every value below tick aligned and comfortably inside
        // the quantity and notional bounds, so the price bound is the only thing any case turns on
        var instrument = new Instrument(
            "fake",
            ProviderEnvironment.Test,
            "XY",
            1m,
            1m,
            1m,
            100m,
            10m,
            100m,
            1m,
            decimal.MaxValue,
            int.MaxValue
        );

        // assert
        instrument.IsValidQtyPrice(1m, 9m).IsFalse("a price below the instrument's minimum is not valid");
        instrument.IsValidQtyPrice(1m, 101m).IsFalse("a price above the instrument's maximum is not valid");
        instrument.IsValidQtyPrice(1m, 10m).IsTrue("the minimum price itself is valid");
        instrument.IsValidQtyPrice(1m, 100m).IsTrue("the maximum price itself is valid");
    }
}
