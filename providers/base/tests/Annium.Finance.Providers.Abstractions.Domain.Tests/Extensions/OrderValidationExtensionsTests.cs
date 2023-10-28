using System;
using Annium.Finance.Providers.Abstractions.Domain.Enums;
using Annium.Finance.Providers.Abstractions.Domain.Extensions;
using Annium.Finance.Providers.Tests.Lib;
using Annium.Finance.Providers.Tests.Lib.Models;
using Annium.Testing;
using NodaTime;
using Xunit;

namespace Annium.Finance.Providers.Abstractions.Domain.Tests.Extensions;

public class OrderValidationExtensionsTests
{
    private readonly Position _position = Helper.CreatePosition(1);

    [Fact]
    public void ValidateSide()
    {
        // arrange
        var order = _position.AddLimitBuyOrder(2, 1).Fill();

        // assert
        order.ValidateSide(OrderSide.Buy);
        Wrap.It(() => order.ValidateSide(OrderSide.Sell)).Throws<InvalidOperationException>();
    }

    [Fact]
    public void ValidateStatus()
    {
        // arrange
        var order = _position.AddLimitBuyOrder(2, 1).FillPartially(1);

        // assert
        order.ValidateStatus(OrderStatus.New, OrderStatus.PartiallyFilled);
        Wrap.It(() => order.ValidateStatus(OrderStatus.Filled, OrderStatus.Canceled))
            .Throws<InvalidOperationException>();
    }

    [Fact]
    public void ValidateQtyAndPrice()
    {
        // assert - total qty
        Wrap.It(() => _position.AddLimitBuyOrder(0, 0))
            .Throws<InvalidOperationException>()
            .Reports("total qty is invalid");

        // assert - level price
        Wrap.It(
                () =>
                    new Order(
                        Guid.NewGuid(),
                        _position,
                        OrderSide.Buy,
                        OrderType.Limit,
                        1,
                        1,
                        1,
                        Instant.MinValue,
                        OrderStatus.New,
                        0,
                        0,
                        0,
                        Instant.MinValue
                    ).ValidateQtyAndPrice()
            )
            .Throws<InvalidOperationException>()
            .Reports("level price is invalid");
        Wrap.It(
                () =>
                    new Order(
                        Guid.NewGuid(),
                        _position,
                        OrderSide.Buy,
                        OrderType.Market,
                        1,
                        1,
                        1,
                        Instant.MinValue,
                        OrderStatus.New,
                        0,
                        0,
                        0,
                        Instant.MinValue
                    ).ValidateQtyAndPrice()
            )
            .Throws<InvalidOperationException>()
            .Reports("level price is invalid");
        Wrap.It(
                () =>
                    new Order(
                        Guid.NewGuid(),
                        _position,
                        OrderSide.Buy,
                        OrderType.TakeProfitMarket,
                        1,
                        0,
                        0,
                        Instant.MinValue,
                        OrderStatus.New,
                        0,
                        0,
                        0,
                        Instant.MinValue
                    ).ValidateQtyAndPrice()
            )
            .Throws<InvalidOperationException>()
            .Reports("level price is invalid");
        Wrap.It(
                () =>
                    new Order(
                        Guid.NewGuid(),
                        _position,
                        OrderSide.Buy,
                        OrderType.StopLossMarket,
                        1,
                        0,
                        0,
                        Instant.MinValue,
                        OrderStatus.New,
                        0,
                        0,
                        0,
                        Instant.MinValue
                    ).ValidateQtyAndPrice()
            )
            .Throws<InvalidOperationException>()
            .Reports("level price is invalid");

        // assert - price
        Wrap.It(
                () =>
                    new Order(
                        Guid.NewGuid(),
                        _position,
                        OrderSide.Buy,
                        OrderType.Limit,
                        1,
                        0,
                        0,
                        Instant.MinValue,
                        OrderStatus.New,
                        0,
                        0,
                        0,
                        Instant.MinValue
                    ).ValidateQtyAndPrice()
            )
            .Throws<InvalidOperationException>()
            .Reports("target price is invalid");
        Wrap.It(
                () =>
                    new Order(
                        Guid.NewGuid(),
                        _position,
                        OrderSide.Buy,
                        OrderType.Market,
                        1,
                        1,
                        0,
                        Instant.MinValue,
                        OrderStatus.New,
                        0,
                        0,
                        0,
                        Instant.MinValue
                    ).ValidateQtyAndPrice()
            )
            .Throws<InvalidOperationException>()
            .Reports("target price is invalid");
        Wrap.It(
                () =>
                    new Order(
                        Guid.NewGuid(),
                        _position,
                        OrderSide.Buy,
                        OrderType.TakeProfitMarket,
                        1,
                        1,
                        1,
                        Instant.MinValue,
                        OrderStatus.New,
                        0,
                        0,
                        0,
                        Instant.MinValue
                    ).ValidateQtyAndPrice()
            )
            .Throws<InvalidOperationException>()
            .Reports("target price is invalid");
        Wrap.It(
                () =>
                    new Order(
                        Guid.NewGuid(),
                        _position,
                        OrderSide.Buy,
                        OrderType.StopLossMarket,
                        1,
                        1,
                        1,
                        Instant.MinValue,
                        OrderStatus.New,
                        0,
                        0,
                        0,
                        Instant.MinValue
                    ).ValidateQtyAndPrice()
            )
            .Throws<InvalidOperationException>()
            .Reports("target price is invalid");

        // assert - new executed qty & price
        Wrap.It(() => _position.AddLimitBuyOrder(3, 2).Update(OrderStatus.New, 1, 0, 0, Instant.MinValue))
            .Throws<InvalidOperationException>()
            .Reports("executed qty is invalid");
        Wrap.It(() => _position.AddLimitBuyOrder(3, 2).Update(OrderStatus.New, 0, 1, 0, Instant.MinValue))
            .Throws<InvalidOperationException>()
            .Reports("executed price is invalid");

        // assert - partially filled executed qty & price
        Wrap.It(() => _position.AddLimitBuyOrder(3, 2).Update(OrderStatus.PartiallyFilled, 0, 1, 0, Instant.MinValue))
            .Throws<InvalidOperationException>()
            .Reports("executed qty is invalid");
        Wrap.It(() => _position.AddLimitBuyOrder(3, 2).Update(OrderStatus.PartiallyFilled, 3, 1, 0, Instant.MinValue))
            .Throws<InvalidOperationException>()
            .Reports("executed qty is invalid");
        Wrap.It(() => _position.AddLimitBuyOrder(3, 2).Update(OrderStatus.PartiallyFilled, 1, 0, 0, Instant.MinValue))
            .Throws<InvalidOperationException>()
            .Reports("executed price is invalid");

        // assert - filled executed qty & price
        Wrap.It(() => _position.AddLimitBuyOrder(3, 2).Update(OrderStatus.Filled, 2, 1, 0, Instant.MinValue))
            .Throws<InvalidOperationException>()
            .Reports("executed qty is invalid");
        Wrap.It(() => _position.AddLimitBuyOrder(3, 2).Update(OrderStatus.Filled, 4, 1, 0, Instant.MinValue))
            .Throws<InvalidOperationException>()
            .Reports("executed qty is invalid");
        Wrap.It(() => _position.AddLimitBuyOrder(3, 2).Update(OrderStatus.Filled, 3, 0, 0, Instant.MinValue))
            .Throws<InvalidOperationException>()
            .Reports("executed price is invalid");

        // assert - canceled executed qty & price
        Wrap.It(() => _position.AddLimitBuyOrder(3, 2).Update(OrderStatus.Canceled, 3, 1, 0, Instant.MinValue))
            .Throws<InvalidOperationException>()
            .Reports("executed qty is invalid");
        Wrap.It(() => _position.AddLimitBuyOrder(3, 2).Update(OrderStatus.Canceled, 0, 1, 0, Instant.MinValue))
            .Throws<InvalidOperationException>()
            .Reports("executed price is invalid");
        Wrap.It(() => _position.AddLimitBuyOrder(3, 2).Update(OrderStatus.Canceled, 1, 0, 0, Instant.MinValue))
            .Throws<InvalidOperationException>()
            .Reports("executed price is invalid");
    }

    [Fact]
    public void ValidateIsExecuted()
    {
        // arrange
        var order = _position.AddLimitBuyOrder(2, 1);

        // assert
        Wrap.It(() => order.ValidateIsExecuted()).Throws<InvalidOperationException>();
        order.FillPartially(1).ValidateIsExecuted();
    }
}
