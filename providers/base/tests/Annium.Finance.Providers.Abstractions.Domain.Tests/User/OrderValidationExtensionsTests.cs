using System;
using Annium.Data.Operations.Testing;
using Annium.Finance.Providers.Abstractions.Domain.Shared.Operations;
using Annium.Finance.Providers.Abstractions.Domain.User;
using Annium.Finance.Providers.Tests.Lib.User;
using Annium.Finance.Providers.Tests.Lib.User.Operations;
using Annium.Testing;
using Xunit;

namespace Annium.Finance.Providers.Abstractions.Domain.Tests.User;

public class OrderValidationExtensionsTests
{
    private readonly Position _position = PositionHelper.CreatePosition(1);

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
