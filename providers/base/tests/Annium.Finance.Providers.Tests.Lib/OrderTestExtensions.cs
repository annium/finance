using Annium.Finance.Providers.Abstractions.Domain.Enums;
using Annium.Finance.Providers.Abstractions.Domain.Extensions;
using Annium.Finance.Providers.Tests.Lib.Models;

namespace Annium.Finance.Providers.Tests.Lib;

public static class OrderTestExtensions
{
    public static Order FillPartially(this Order order, decimal executedQty)
    {
        order.ValidateIsLimit();
        var qty = order.ExecutedQty + executedQty;

        return order.Update(OrderStatus.PartiallyFilled, qty, order.Price, qty * order.Price.Fee(), 0);
    }

    public static Order FillPartially(this Order order, decimal executedQty, decimal price)
    {
        order.ValidateIsMarket();
        var qty = order.ExecutedQty + executedQty;

        return order.Update(OrderStatus.PartiallyFilled, qty, price, qty * price.Fee(), 0);
    }

    public static Order Fill(this Order order)
    {
        order.ValidateIsLimit();

        return order.Update(OrderStatus.Filled, order.TotalQty, order.Price, order.TotalQty * order.Price.Fee(), 0);
    }

    public static Order Fill(this Order order, decimal price)
    {
        order.ValidateIsMarket();

        return order.Update(OrderStatus.Filled, order.TotalQty, price, order.TotalQty * price.Fee(), 0);
    }

    public static Order Cancel(this Order order)
    {
        order.ValidateIsLimit();

        return order.Update(
            OrderStatus.Canceled,
            order.ExecutedQty,
            order.ExecutedQty == 0 ? 0 : order.Price,
            order.Fee,
            0
        );
    }

    public static Order Cancel(this Order order, decimal price)
    {
        order.ValidateIsMarket();

        return order.Update(OrderStatus.Canceled, order.ExecutedQty, order.ExecutedQty == 0 ? 0 : price, order.Fee, 0);
    }

    public static Order AddToPosition(this Order order)
    {
        order.ValidateStatus(OrderStatus.New);
        order.ValidateQtyAndPrice();

        order.Position.AddOrder(order.Id, order.Side, order.TotalQty, order.CreatedAt);

        return order;
    }
}
