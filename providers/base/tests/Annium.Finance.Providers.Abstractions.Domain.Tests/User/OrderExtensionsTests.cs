using Annium.Finance.Providers.Abstractions.Domain.User;
using Annium.Finance.Providers.Tests.Lib.User;
using Annium.Finance.Providers.Tests.Lib.User.Operations;
using Annium.Testing;
using Xunit;

namespace Annium.Finance.Providers.Abstractions.Domain.Tests.User;

/// <summary>
/// Pins the status and type classifications <see cref="OrderExtensions"/> derives from an <see cref="IOrder"/>,
/// exercised across the order lifecycle produced by the <c>Tests.Lib</c> position/order fixtures.
/// </summary>
public class OrderExtensionsTests
{
    /// <summary>Verifies that <see cref="OrderExtensions.IsActive{TOrder}"/> is true for new and partially filled orders, false once filled or canceled.</summary>
    [Fact]
    public void IsActive()
    {
        // arrange
        var position = PositionHelper.CreatePosition(1);

        // assert
        position.AddLimitBuyOrder(2, 1).Data.IsActive().IsTrue();
        position.AddLimitBuyOrder(2, 1).FillPartially(1).Data.IsActive().IsTrue();
        position.AddLimitBuyOrder(2, 1).Fill().Data.IsActive().IsFalse();
        position.AddLimitBuyOrder(2, 1).Cancel().Data.IsActive().IsFalse();
    }

    /// <summary>Verifies that <see cref="OrderExtensions.IsInactive{TOrder}"/> is the exact inverse of <see cref="OrderExtensions.IsActive{TOrder}"/>.</summary>
    [Fact]
    public void IsInactive()
    {
        // arrange
        var position = PositionHelper.CreatePosition(1);

        // assert
        position.AddLimitBuyOrder(2, 1).Data.IsInactive().IsFalse();
        position.AddLimitBuyOrder(2, 1).FillPartially(1).Data.IsInactive().IsFalse();
        position.AddLimitBuyOrder(2, 1).Fill().Data.IsInactive().IsTrue();
        position.AddLimitBuyOrder(2, 1).Cancel().Data.IsInactive().IsTrue();
    }

    /// <summary>Verifies that <see cref="OrderExtensions.IsImmediate{TOrder}"/> is true for a limit order but false for stop-loss and take-profit orders.</summary>
    [Fact]
    public void IsImmediate()
    {
        // arrange
        var position = PositionHelper.CreatePosition(1);

        // assert
        position.AddLimitBuyOrder(2, 1).Fill().Data.IsImmediate().IsTrue();
        position.AddStopLossMarketSellOrder(1, 0.5m).Data.IsImmediate().IsFalse();
        position.AddTakeProfitMarketSellOrder(1, 1.5m).Data.IsImmediate().IsFalse();
    }

    /// <summary>
    /// Verifies leveled/immediate classification via <see cref="OrderExtensions.IsLimit{TOrder}"/> as a stand-in
    /// check: a limit order is not leveled, while stop-loss and take-profit market orders are.
    /// </summary>
    [Fact]
    public void IsLeveled()
    {
        // arrange
        var position = PositionHelper.CreatePosition(1);

        // assert
        position.AddLimitBuyOrder(2, 1).Fill().Data.IsLimit().IsTrue();
        position.AddStopLossMarketSellOrder(1, 0.5m).Data.IsLimit().IsFalse();
        position.AddTakeProfitMarketSellOrder(1, 1.5m).Data.IsLimit().IsFalse();
    }

    /// <summary>Verifies that <see cref="OrderExtensions.IsLimit{TOrder}"/> is true only for a limit order, false for stop-loss and take-profit market orders.</summary>
    [Fact]
    public void IsLimit()
    {
        // arrange
        var position = PositionHelper.CreatePosition(1);

        // assert
        position.AddLimitBuyOrder(2, 1).Fill().Data.IsLimit().IsTrue();
        position.AddStopLossMarketSellOrder(1, 0.5m).Data.IsLimit().IsFalse();
        position.AddTakeProfitMarketSellOrder(1, 1.5m).Data.IsLimit().IsFalse();
    }

    /// <summary>Verifies that <see cref="OrderExtensions.IsMarket{TOrder}"/> is false for a limit order but true for stop-loss and take-profit market orders.</summary>
    [Fact]
    public void IsMarket()
    {
        // arrange
        var position = PositionHelper.CreatePosition(1);

        // assert
        position.AddLimitBuyOrder(2, 1).Fill().Data.IsMarket().IsFalse();
        position.AddStopLossMarketSellOrder(1, 0.5m).Data.IsMarket().IsTrue();
        position.AddTakeProfitMarketSellOrder(1, 1.5m).Data.IsMarket().IsTrue();
    }

    /// <summary>
    /// Verifies that <see cref="OrderExtensions.OpeningQty{TOrder}"/> tracks the unfilled quantity while the order
    /// is active, and drops to zero once the order fills or is canceled - regardless of how much was filled
    /// before the cancel.
    /// </summary>
    [Fact]
    public void OpeningQty()
    {
        // arrange
        var position = PositionHelper.CreatePosition(1);

        // assert
        position.AddLimitBuyOrder(2, 1).Data.OpeningQty().Is(2);
        position.AddLimitBuyOrder(2, 1).FillPartially(1).Data.OpeningQty().Is(1);
        position.AddLimitBuyOrder(2, 1).Fill().Data.OpeningQty().Is(0);
        position.AddLimitBuyOrder(2, 1).FillPartially(0.5m).Cancel().Data.OpeningQty().Is(0);
    }
}
