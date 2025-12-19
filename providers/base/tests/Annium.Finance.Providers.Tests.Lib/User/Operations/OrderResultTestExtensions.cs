using Annium.Data.Operations;
using Annium.Finance.Providers.Abstractions.Domain.User;

namespace Annium.Finance.Providers.Tests.Lib.User.Operations;

public static class OrderResultTestExtensions
{
    public static IResult<Order> FillPartially(this IResult<Order> result, decimal executedQty)
    {
        if (result.HasErrors)
            return result;

        var qty = result.Data.ExecutedQty + executedQty;

        return result
            .ValidateIsLimit()
            .Join(
                result.Data.Update(
                    OrderStatus.PartiallyFilled,
                    qty,
                    result.Data.Price,
                    qty * result.Data.Price.Fee(),
                    0
                )
            );
    }

    public static IResult<Order> FillPartially(this IResult<Order> result, decimal executedQty, decimal price)
    {
        if (result.HasErrors)
            return result;

        var qty = result.Data.ExecutedQty + executedQty;

        return result
            .ValidateIsMarket()
            .Join(result.Data.Update(OrderStatus.PartiallyFilled, qty, price, qty * price.Fee(), 0));
    }

    public static IResult<Order> Fill(this IResult<Order> result)
    {
        if (result.HasErrors)
            return result;

        return result
            .ValidateIsLimit()
            .Join(
                result.Data.Update(
                    OrderStatus.Filled,
                    result.Data.TotalQty,
                    result.Data.Price,
                    result.Data.TotalQty * result.Data.Price.Fee(),
                    0
                )
            );
    }

    public static IResult<Order> Fill(this IResult<Order> result, decimal price)
    {
        if (result.HasErrors)
            return result;

        return result
            .ValidateIsMarket()
            .Join(
                result.Data.Update(
                    OrderStatus.Filled,
                    result.Data.TotalQty,
                    price,
                    result.Data.TotalQty * price.Fee(),
                    0
                )
            );
    }

    public static IResult<Order> Cancel(this IResult<Order> result)
    {
        if (result.HasErrors)
            return result;

        return result
            .ValidateIsLimit()
            .Join(
                result.Data.Update(
                    OrderStatus.Canceled,
                    result.Data.ExecutedQty,
                    result.Data.ExecutedQty == 0 ? 0 : result.Data.Price,
                    result.Data.Fee,
                    0
                )
            );
    }

    public static IResult<Order> Cancel(this IResult<Order> result, decimal price)
    {
        if (result.HasErrors)
            return result;

        return result
            .ValidateIsMarket()
            .Join(
                result.Data.Update(
                    OrderStatus.Canceled,
                    result.Data.ExecutedQty,
                    result.Data.ExecutedQty == 0 ? 0 : price,
                    result.Data.Fee,
                    0
                )
            );
    }
}
