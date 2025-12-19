using Annium.Finance.Providers.Abstractions.Domain.Market;
using Annium.Finance.Providers.Tests.Lib.Helpers;
using Annium.Testing;
using Xunit;

namespace Annium.Finance.Providers.Abstractions.Domain.Tests.Extensions;

public class InstrumentExtensionsTests
{
    private readonly IInstrument _instrument = Helper.DefaultInstrument;

    [Fact]
    public void TickPrecision()
    {
        // assert
        Helper.CreateInstrument("x", "y", 100m, 10m).TickPrecision().Is(0);
        Helper.CreateInstrument("x", "y", 75m, 7.50m).TickPrecision().Is(1);
        Helper.CreateInstrument("x", "y", 7.5m, 0.75m).TickPrecision().Is(2);
        Helper.CreateInstrument("x", "y", 1m, 0.01m).TickPrecision().Is(2);
    }

    [Fact]
    public void ToValidQty()
    {
        // assert
        _instrument.ToValidQty(1.56m).Is(1.5m);
        _instrument.ToValidQty(0.05m).Is(_instrument.MinQty);
        _instrument.ToValidQty(105m).Is(_instrument.MaxQty);
    }

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

    [Fact]
    public void ToTickSizeDown()
    {
        // assert
        _instrument.ToTickSizeDown(0.126m).Is(0.12m);
    }

    [Fact]
    public void ToTickSizeRound()
    {
        // arrange
        _instrument.ToTickSizeRound(0.124m).Is(0.12m);
        _instrument.ToTickSizeRound(0.126m).Is(0.13m);
    }

    [Fact]
    public void ToTickSizeUp()
    {
        // arrange
        _instrument.ToTickSizeUp(0.124m).Is(0.13m);
    }

    [Fact]
    public void ToLotSize()
    {
        // arrange
        _instrument.ToLotSize(0.12m).Is(0.1m);
    }
}
