using Annium.Finance.Providers.Abstractions.Connectors.User;
using Annium.Finance.Providers.Abstractions.Domain.Market;
using Annium.Finance.Providers.Abstractions.Domain.User;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.User;

/// <summary>
/// USD-M futures implementation of <see cref="IFinanceService"/>. USD-M contracts are linear (quoted and settled
/// in the quote asset), so cost/value/quantity are plain <c>qty * price</c> relations scaled by leverage, unlike
/// the inverse contracts used elsewhere in Binance's futures products.
/// </summary>
internal class FinanceService : IFinanceService
{
    /// <summary>
    /// Calculates the P&amp;L result of executing an order at the given price and quantity against a position
    /// with the given orientation and price, without fees.
    /// </summary>
    /// <param name="instrument">The traded instrument.</param>
    /// <param name="orientation">The orientation (long/short) of the position the order belongs to.</param>
    /// <param name="leverage">The leverage applied to the position.</param>
    /// <param name="positionPrice">The position's opened price.</param>
    /// <param name="side">The side (buy/sell) of the order.</param>
    /// <param name="qty">The order quantity.</param>
    /// <param name="price">The order execution price.</param>
    /// <returns>The order's result, excluding fees.</returns>
    public decimal GetResult(
        IInstrument instrument,
        Orientation orientation,
        decimal leverage,
        decimal positionPrice,
        OrderSide side,
        decimal qty,
        decimal price
    )
    {
        var leveragedPart = 1m / leverage;

        // for open order result is leveraged expense sum
        if (side == orientation.OpenSide)
        {
            var expense = qty * price * leveragedPart;
            return -expense;
        }

        var openedValue = qty * positionPrice * leveragedPart;
        var priceDiff = orientation == Orientation.Long ? price - positionPrice : positionPrice - price;
        var pnl = qty * priceDiff;
        var income = openedValue + pnl;

        return income;
    }

    /// <summary>
    /// Calculates the cost of purchasing the given quantity of an instrument at the given price and leverage,
    /// without fees.
    /// </summary>
    /// <param name="instrument">The traded instrument.</param>
    /// <param name="leverage">The leverage applied to the purchase.</param>
    /// <param name="side">The side (buy/sell) of the order.</param>
    /// <param name="qty">The quantity to purchase.</param>
    /// <param name="price">The execution price.</param>
    /// <returns>The purchase cost, excluding fees.</returns>
    public decimal GetCost(IInstrument instrument, decimal leverage, OrderSide side, decimal qty, decimal price)
    {
        return qty * price / leverage;
    }

    /// <summary>
    /// Calculates the sum that would be borrowed from the provider when purchasing the given quantity of an
    /// instrument at the given price and leverage, without fees.
    /// </summary>
    /// <param name="instrument">The traded instrument.</param>
    /// <param name="leverage">The leverage applied to the purchase.</param>
    /// <param name="side">The side (buy/sell) of the order.</param>
    /// <param name="qty">The quantity to purchase.</param>
    /// <param name="price">The execution price.</param>
    /// <returns>The borrowed sum, excluding fees.</returns>
    public decimal GetBorrowedSum(IInstrument instrument, decimal leverage, OrderSide side, decimal qty, decimal price)
    {
        if (leverage == 0)
            return 0;

        return qty * price * (leverage - 1) / leverage;
    }

    /// <summary>
    /// Calculates the value of the given quantity of an instrument at the given price and leverage, without fees.
    /// </summary>
    /// <param name="instrument">The traded instrument.</param>
    /// <param name="leverage">The leverage applied to the position.</param>
    /// <param name="side">The side (buy/sell) of the order.</param>
    /// <param name="qty">The quantity to value.</param>
    /// <param name="price">The execution price.</param>
    /// <returns>The value, excluding fees.</returns>
    public decimal GetValue(IInstrument instrument, decimal leverage, OrderSide side, decimal qty, decimal price)
    {
        return qty * price / leverage;
    }

    /// <summary>
    /// Calculates the quantity of an instrument purchasable with the given sum at the given price and leverage,
    /// without fees.
    /// </summary>
    /// <param name="instrument">The traded instrument.</param>
    /// <param name="leverage">The leverage applied to the purchase.</param>
    /// <param name="side">The side (buy/sell) of the order.</param>
    /// <param name="sum">The sum available to spend.</param>
    /// <param name="price">The execution price.</param>
    /// <returns>The purchasable quantity, excluding fees.</returns>
    public decimal GetQty(IInstrument instrument, decimal leverage, OrderSide side, decimal sum, decimal price)
    {
        return sum * leverage / price;
    }
}
