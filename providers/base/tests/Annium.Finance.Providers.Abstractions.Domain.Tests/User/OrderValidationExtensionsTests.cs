using System;
using Annium.Data.Operations.Testing;
using Annium.Finance.Providers.Abstractions.Domain.User;
using Annium.Finance.Providers.Tests.Lib.Shared.Operations;
using Annium.Finance.Providers.Tests.Lib.User;
using Annium.Finance.Providers.Tests.Lib.User.Operations;
using Annium.Testing;
using Xunit;

namespace Annium.Finance.Providers.Abstractions.Domain.Tests.User;

/// <summary>
/// Pins the fluent validation checks in <see cref="OrderValidationExtensions"/>: each single-property check
/// (side, active/inactive, immediate/leveled, limit/market, status) appends the expected error message when it
/// fails, and <c>ValidateCanProcess</c> catches every way an order's terms and a candidate status/qty/price
/// update can be internally inconsistent.
/// </summary>
public class OrderValidationExtensionsTests
{
    /// <summary>A fresh position with a total quantity of 1, used as the base for every order these tests validate.</summary>
    private readonly Position _position = PositionHelper.CreatePosition(1);

    /// <summary>Verifies that <see cref="OrderValidationExtensions.ValidateSide{TOrder}"/> passes for the order's actual side and appends an error for any other side.</summary>
    [Fact]
    public void ValidateSide()
    {
        // arrange
        var result = _position.AddLimitBuyOrder(2, 1).Fill();
        result.HasNoErrors();

        // assert
        result.ValidateSide(OrderSide.Buy);
        result.HasNoErrors();
        result.ValidateSide(OrderSide.Sell);
        result.HasErrors();
        result.PlainErrors.At(0).IsContaining($"not a {OrderSide.Sell}");
    }

    /// <summary>Verifies that <see cref="OrderValidationExtensions.ValidateIsActive{TOrder}"/> passes for a partially filled order and appends an error for a canceled one.</summary>
    [Fact]
    public void ValidateIsActive()
    {
        // arrange
        var activeResult = _position.AddLimitBuyOrder(2, 1).FillPartially(1);
        var inactiveResult = _position.AddLimitBuyOrder(2, 1).Cancel();

        // assert
        activeResult.ValidateIsActive().HasNoErrors();
        inactiveResult.ValidateIsActive().PlainErrors.At(0).IsContaining("is not Active");
    }

    /// <summary>Verifies that <see cref="OrderValidationExtensions.ValidateIsInactive{TOrder}"/> passes for a canceled order and appends an error for a partially filled one.</summary>
    [Fact]
    public void ValidateIsInactive()
    {
        // arrange
        var activeResult = _position.AddLimitBuyOrder(2, 1).FillPartially(1);
        var inactiveResult = _position.AddLimitBuyOrder(2, 1).Cancel();

        // assert
        inactiveResult.ValidateIsInactive();
        activeResult.ValidateIsInactive().PlainErrors.At(0).IsContaining("is not Inactive");
    }

    /// <summary>Verifies that <see cref="OrderValidationExtensions.ValidateIsImmediate{TOrder}"/> passes for a limit order and appends an error for a leveled (stop-loss) order.</summary>
    [Fact]
    public void ValidateIsImmediate()
    {
        // arrange
        var immediateOrder = _position.AddLimitBuyOrder(2, 1);
        var leveledOrder = _position.AddStopLossMarketBuyOrder(2, 1);
        immediateOrder.HasNoErrors();
        leveledOrder.HasNoErrors();

        // assert
        immediateOrder.ValidateIsImmediate().HasNoErrors();
        leveledOrder.ValidateIsImmediate().PlainErrors.At(0).IsContaining("is not an immediate order");
    }

    /// <summary>Verifies that <see cref="OrderValidationExtensions.ValidateIsLeveled{TOrder}"/> passes for a leveled (stop-loss) order and appends an error for an immediate (limit) order.</summary>
    [Fact]
    public void ValidateIsLeveled()
    {
        // arrange
        var leveledOrder = _position.AddStopLossMarketBuyOrder(2, 1);
        var immediateOrder = _position.AddLimitBuyOrder(2, 1);
        leveledOrder.HasNoErrors();
        immediateOrder.HasNoErrors();

        // assert
        leveledOrder.ValidateIsLeveled().HasNoErrors();
        immediateOrder.ValidateIsLeveled().PlainErrors.At(0).IsContaining("is not a leveled order");
    }

    /// <summary>Verifies that <see cref="OrderValidationExtensions.ValidateIsMarket{TOrder}"/> passes for a market order and appends an error for a limit order.</summary>
    [Fact]
    public void ValidateIsMarket()
    {
        // arrange
        var marketOrder = _position.AddMarketBuyOrder(2);
        var limitOrder = _position.AddLimitBuyOrder(2, 1);
        marketOrder.HasNoErrors();
        limitOrder.HasNoErrors();

        // assert
        marketOrder.ValidateIsMarket().HasNoErrors();
        limitOrder.ValidateIsMarket().PlainErrors.At(0).IsContaining("is not a market order");
    }

    /// <summary>
    /// Verifies every arity of <c>ValidateStatus</c> (one through four accepted statuses): each passes when the
    /// order's status is among the given ones, and appends an error listing all of them when it is not.
    /// </summary>
    [Fact]
    public void ValidateStatus()
    {
        // arrange
        var result = _position.AddLimitBuyOrder(2, 1).FillPartially(1);

        // assert
        result.ValidateStatus(OrderStatus.New, OrderStatus.PartiallyFilled);
        result
            .ValidateStatus(OrderStatus.Filled, OrderStatus.Canceled)
            .PlainErrors.At(0)
            .IsContaining($"is not {OrderStatus.Filled}, {OrderStatus.Canceled}");

        var newOrder = _position.AddLimitBuyOrder(2, 1);
        newOrder.HasNoErrors();
        newOrder.ValidateStatus(OrderStatus.New).HasNoErrors();
        newOrder.ValidateStatus(OrderStatus.Filled).PlainErrors.At(0).IsContaining($"is not {OrderStatus.Filled}");

        var canceledOrder = _position.AddLimitBuyOrder(2, 1).Cancel();
        canceledOrder.HasNoErrors();
        canceledOrder.ValidateStatus(OrderStatus.New, OrderStatus.PartiallyFilled, OrderStatus.Canceled).HasNoErrors();
        canceledOrder
            .ValidateStatus(OrderStatus.New, OrderStatus.PartiallyFilled, OrderStatus.Filled, OrderStatus.Canceled)
            .HasNoErrors();
        canceledOrder
            .ValidateStatus(OrderStatus.New, OrderStatus.PartiallyFilled, OrderStatus.Filled)
            .PlainErrors.At(0)
            .IsContaining($"is not {OrderStatus.New}, {OrderStatus.PartiallyFilled}, {OrderStatus.Filled}");
    }

    /// <summary>
    /// Verifies every failure path of <c>ValidateCanProcess</c> and its status-aware overload: a non-positive
    /// total quantity, a level price required or forbidden by the order's leveled/immediate kind, a target price
    /// required by its limit/market kind, and - per candidate status (new, partially filled, filled, canceled) -
    /// an executed quantity or price outside the range that status allows.
    /// </summary>
    [Fact]
    public void ValidateCanProcess()
    {
        // assert - total qty
        new Order(Guid.NewGuid(), _position, OrderSide.Buy, OrderType.Limit, 0, 1, 1, 0, OrderStatus.New, 0, 0, 0, 0)
            .AsResult()
            .ValidateCanProcess()
            .PlainErrors.At(0)
            .IsContaining("total qty is invalid");

        // assert - level price
        new Order(Guid.NewGuid(), _position, OrderSide.Buy, OrderType.Limit, 1, 1, 1, 0, OrderStatus.New, 0, 0, 0, 0)
            .AsResult()
            .ValidateCanProcess()
            .PlainErrors.At(0)
            .IsContaining("level price is invalid");

        new Order(Guid.NewGuid(), _position, OrderSide.Buy, OrderType.Market, 1, 1, 1, 0, OrderStatus.New, 0, 0, 0, 0)
            .AsResult()
            .ValidateCanProcess()
            .PlainErrors.At(0)
            .IsContaining("level price is invalid");

        new Order(
            Guid.NewGuid(),
            _position,
            OrderSide.Buy,
            OrderType.TakeProfitMarket,
            1,
            0,
            0,
            0,
            OrderStatus.New,
            0,
            0,
            0,
            0
        )
            .AsResult()
            .ValidateCanProcess()
            .PlainErrors.At(0)
            .IsContaining("level price is invalid");

        new Order(
            Guid.NewGuid(),
            _position,
            OrderSide.Buy,
            OrderType.StopLossMarket,
            1,
            0,
            0,
            0,
            OrderStatus.New,
            0,
            0,
            0,
            0
        )
            .AsResult()
            .ValidateCanProcess()
            .PlainErrors.At(0)
            .IsContaining("level price is invalid");

        // assert - price

        new Order(Guid.NewGuid(), _position, OrderSide.Buy, OrderType.Limit, 1, 0, 0, 0, OrderStatus.New, 0, 0, 0, 0)
            .AsResult()
            .ValidateCanProcess()
            .PlainErrors.At(0)
            .IsContaining("target price is invalid");

        new Order(Guid.NewGuid(), _position, OrderSide.Buy, OrderType.Market, 1, 1, 0, 0, OrderStatus.New, 0, 0, 0, 0)
            .AsResult()
            .ValidateCanProcess()
            .PlainErrors.At(0)
            .IsContaining("target price is invalid");

        new Order(
            Guid.NewGuid(),
            _position,
            OrderSide.Buy,
            OrderType.TakeProfitMarket,
            1,
            1,
            1,
            0,
            OrderStatus.New,
            0,
            0,
            0,
            0
        )
            .AsResult()
            .ValidateCanProcess()
            .PlainErrors.At(0)
            .IsContaining("target price is invalid");

        new Order(
            Guid.NewGuid(),
            _position,
            OrderSide.Buy,
            OrderType.StopLossMarket,
            1,
            1,
            1,
            0,
            OrderStatus.New,
            0,
            0,
            0,
            0
        )
            .AsResult()
            .ValidateCanProcess()
            .PlainErrors.At(0)
            .IsContaining("target price is invalid");

        // assert - new executed qty & price
        var result = _position.AddLimitBuyOrder(3, 2);
        result.HasNoErrors();
        result = result.Data.Update(OrderStatus.New, 1, 0, 0, 0);
        result.PlainErrors.At(0).IsContaining("executed qty is invalid");

        result = _position.AddLimitBuyOrder(3, 2);
        result.HasNoErrors();
        result = result.Data.Update(OrderStatus.New, 0, 1, 0, 0);
        result.PlainErrors.At(0).IsContaining("executed price is invalid");

        // assert - partially filled executed qty & price
        result = _position.AddLimitBuyOrder(3, 2);
        result.HasNoErrors();
        result = result.Data.Update(OrderStatus.PartiallyFilled, 0, 1, 0, 0);
        result.PlainErrors.At(0).IsContaining("executed qty is invalid");

        result = _position.AddLimitBuyOrder(3, 2);
        result.HasNoErrors();
        result.Data.Update(OrderStatus.PartiallyFilled, 2, 1, 0, 0);
        result = result.Data.Update(OrderStatus.PartiallyFilled, 1, 1, 0, 0);
        result.PlainErrors.At(0).IsContaining("executed qty is invalid");

        result = _position.AddLimitBuyOrder(3, 2);
        result.HasNoErrors();
        result = result.Data.Update(OrderStatus.PartiallyFilled, 3, 1, 0, 0);
        result.PlainErrors.At(0).IsContaining("executed qty is invalid");

        result = _position.AddLimitBuyOrder(3, 2);
        result.HasNoErrors();
        result = result.Data.Update(OrderStatus.PartiallyFilled, 1, 0, 0, 0);
        result.PlainErrors.At(0).IsContaining("executed price is invalid");

        // assert - filled executed qty & price
        result = _position.AddLimitBuyOrder(3, 2);
        result.HasNoErrors();
        result = result.Data.Update(OrderStatus.Filled, 2, 1, 0, 0);
        result.PlainErrors.At(0).IsContaining("executed qty is invalid");

        result = _position.AddLimitBuyOrder(3, 2);
        result.HasNoErrors();
        result = result.Data.Update(OrderStatus.Filled, 4, 1, 0, 0);
        result.PlainErrors.At(0).IsContaining("executed qty is invalid");

        result = _position.AddLimitBuyOrder(3, 2);
        result.HasNoErrors();
        result = result.Data.Update(OrderStatus.Filled, 3, 0, 0, 0);
        result.PlainErrors.At(0).IsContaining("executed price is invalid");

        // assert - declined executed qty & price
        result = _position.AddLimitBuyOrder(3, 2);
        result.HasNoErrors();
        result = result.Data.Update(OrderStatus.Canceled, 3, 1, 0, 0);
        result.PlainErrors.At(0).IsContaining("executed qty is invalid");

        result = _position.AddLimitBuyOrder(3, 2);
        result.HasNoErrors();
        result = result.Data.Update(OrderStatus.Canceled, 0, 1, 0, 0);
        result.PlainErrors.At(0).IsContaining("executed price is invalid");

        result = _position.AddLimitBuyOrder(3, 2);
        result.HasNoErrors();
        result.Data.Update(OrderStatus.PartiallyFilled, 2, 1, 0, 0);
        result = result.Data.Update(OrderStatus.Canceled, 1, 1, 0, 0);
        result.PlainErrors.At(0).IsContaining("executed qty is invalid");

        result = _position.AddLimitBuyOrder(3, 2);
        result.HasNoErrors();
        result = result.Data.Update(OrderStatus.Canceled, 1, 0, 0, 0);
        result.PlainErrors.At(0).IsContaining("executed price is invalid");
    }

    /// <summary>Verifies that <see cref="OrderValidationExtensions.ValidateIsExecuted{TOrder}"/> appends an error for an order with no fills yet, and passes once it has been partially filled.</summary>
    [Fact]
    public void ValidateIsExecuted()
    {
        // arrange
        var result = _position.AddLimitBuyOrder(2, 1);

        // assert
        result.ValidateIsExecuted().PlainErrors.At(0).IsContaining("has not been executed");
        result.FillPartially(1).ValidateIsExecuted();
    }
}
