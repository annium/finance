using Annium.Data.Operations;
using Annium.Finance.Providers.Abstractions.Domain.User;

namespace Annium.Finance.Providers.Tests.Lib.User.Operations;

/// <summary>
/// Advances a fake <see cref="Order"/> wrapped in a result through fills and cancellation, applying the fee
/// that matching order and quantity would incur, so tests can build up the expected local order state and
/// diff it against what a connector reports.
/// </summary>
public static class OrderResultTestExtensions
{
    /// <summary>
    /// Partially fills a limit order at its own price, using the order's current fill as the starting point.
    /// </summary>
    /// <param name="result">The order result to advance; passed through unchanged if it already has errors.</param>
    /// <param name="executedQty">The additional quantity filled by this call.</param>
    /// <returns>The result with the order updated to partially filled, or the original errors.</returns>
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

    /// <summary>
    /// Partially fills a market order at the given price, using the order's current fill as the starting point.
    /// </summary>
    /// <param name="result">The order result to advance; passed through unchanged if it already has errors.</param>
    /// <param name="executedQty">The additional quantity filled by this call.</param>
    /// <param name="price">The price the additional quantity was filled at.</param>
    /// <returns>The result with the order updated to partially filled, or the original errors.</returns>
    public static IResult<Order> FillPartially(this IResult<Order> result, decimal executedQty, decimal price)
    {
        if (result.HasErrors)
            return result;

        var qty = result.Data.ExecutedQty + executedQty;

        return result
            .ValidateIsMarket()
            .Join(result.Data.Update(OrderStatus.PartiallyFilled, qty, price, qty * price.Fee(), 0));
    }

    /// <summary>
    /// Fully fills a limit order at its own price.
    /// </summary>
    /// <param name="result">The order result to advance; passed through unchanged if it already has errors.</param>
    /// <returns>The result with the order updated to filled, or the original errors.</returns>
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

    /// <summary>
    /// Fully fills a market order at the given price.
    /// </summary>
    /// <param name="result">The order result to advance; passed through unchanged if it already has errors.</param>
    /// <param name="price">The price the order was filled at.</param>
    /// <returns>The result with the order updated to filled, or the original errors.</returns>
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

    /// <summary>
    /// Cancels a limit order, keeping whatever quantity it had already filled.
    /// </summary>
    /// <param name="result">The order result to advance; passed through unchanged if it already has errors.</param>
    /// <returns>The result with the order updated to canceled, or the original errors.</returns>
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

    /// <summary>
    /// Cancels a market order, keeping whatever quantity it had already filled at the given price.
    /// </summary>
    /// <param name="result">The order result to advance; passed through unchanged if it already has errors.</param>
    /// <param name="price">The price the already-filled quantity was filled at.</param>
    /// <returns>The result with the order updated to canceled, or the original errors.</returns>
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
