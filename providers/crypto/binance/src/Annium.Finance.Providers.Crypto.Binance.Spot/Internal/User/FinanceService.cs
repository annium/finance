using System;
using Annium.Finance.Providers.Abstractions.Connectors.User;
using Annium.Finance.Providers.Abstractions.Domain.Market;
using Annium.Finance.Providers.Abstractions.Domain.User;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Internal.User;

/// <summary>
/// Spot implementation of <see cref="IFinanceService"/>. Spot trading has no leveraged positions, so none of
/// the leverage-aware sizing/valuing calculations are implemented yet.
/// </summary>
internal class FinanceService : IFinanceService
{
    /// <summary>Not implemented for spot.</summary>
    /// <param name="instrument">The traded instrument.</param>
    /// <param name="orientation">The orientation (long/short) of the position the order belongs to.</param>
    /// <param name="leverage">The leverage applied to the position.</param>
    /// <param name="positionPrice">The position's opened price.</param>
    /// <param name="side">The side (buy/sell) of the order.</param>
    /// <param name="qty">The order quantity.</param>
    /// <param name="price">The order execution price.</param>
    /// <returns>Does not return; always throws.</returns>
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
        throw new NotImplementedException();
    }

    /// <summary>Not implemented for spot.</summary>
    /// <param name="instrument">The traded instrument.</param>
    /// <param name="leverage">The leverage applied to the purchase.</param>
    /// <param name="side">The side (buy/sell) of the order.</param>
    /// <param name="qty">The quantity to purchase.</param>
    /// <param name="price">The execution price.</param>
    /// <returns>Does not return; always throws.</returns>
    public decimal GetCost(IInstrument instrument, decimal leverage, OrderSide side, decimal qty, decimal price)
    {
        throw new NotImplementedException();
    }

    /// <summary>Not implemented for spot.</summary>
    /// <param name="instrument">The traded instrument.</param>
    /// <param name="leverage">The leverage applied to the purchase.</param>
    /// <param name="side">The side (buy/sell) of the order.</param>
    /// <param name="qty">The quantity to purchase.</param>
    /// <param name="price">The execution price.</param>
    /// <returns>Does not return; always throws.</returns>
    public decimal GetBorrowedSum(IInstrument instrument, decimal leverage, OrderSide side, decimal qty, decimal price)
    {
        throw new NotImplementedException();
    }

    /// <summary>Not implemented for spot.</summary>
    /// <param name="instrument">The traded instrument.</param>
    /// <param name="leverage">The leverage applied to the position.</param>
    /// <param name="side">The side (buy/sell) of the order.</param>
    /// <param name="qty">The quantity to value.</param>
    /// <param name="price">The execution price.</param>
    /// <returns>Does not return; always throws.</returns>
    public decimal GetValue(IInstrument instrument, decimal leverage, OrderSide side, decimal qty, decimal price)
    {
        throw new NotImplementedException();
    }

    /// <summary>Not implemented for spot.</summary>
    /// <param name="instrument">The traded instrument.</param>
    /// <param name="leverage">The leverage applied to the purchase.</param>
    /// <param name="side">The side (buy/sell) of the order.</param>
    /// <param name="sum">The sum available to spend.</param>
    /// <param name="price">The execution price.</param>
    /// <returns>Does not return; always throws.</returns>
    public decimal GetQty(IInstrument instrument, decimal leverage, OrderSide side, decimal sum, decimal price)
    {
        throw new NotImplementedException();
    }
}
