using Annium.Finance.Providers.Abstractions.Domain.Market;
using Annium.Finance.Providers.Abstractions.Domain.User;

namespace Annium.Finance.Providers.Abstractions.Connectors.User;

/// <summary>
/// Leverage-aware arithmetic for sizing and valuing trades on an instrument. None of these calculations take
/// fees into account.
/// </summary>
public interface IFinanceService
{
    /// <summary>
    /// Calculates the P&amp;L result of executing an order at the given price and quantity against a position with
    /// the given orientation and price, without fees.
    /// </summary>
    /// <param name="instrument">The traded instrument.</param>
    /// <param name="orientation">The orientation (long/short) of the position the order belongs to.</param>
    /// <param name="leverage">The leverage applied to the position.</param>
    /// <param name="positionPrice">The position's opened price.</param>
    /// <param name="side">The side (buy/sell) of the order.</param>
    /// <param name="qty">The order quantity.</param>
    /// <param name="price">The order execution price.</param>
    /// <returns>The order's result, excluding fees.</returns>
    decimal GetResult(
        IInstrument instrument,
        Orientation orientation,
        decimal leverage,
        decimal positionPrice,
        OrderSide side,
        decimal qty,
        decimal price
    );

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
    decimal GetCost(IInstrument instrument, decimal leverage, OrderSide side, decimal qty, decimal price);

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
    decimal GetBorrowedSum(IInstrument instrument, decimal leverage, OrderSide side, decimal qty, decimal price);

    /// <summary>
    /// Calculates the value of the given quantity of an instrument at the given price and leverage, without fees.
    /// </summary>
    /// <param name="instrument">The traded instrument.</param>
    /// <param name="leverage">The leverage applied to the position.</param>
    /// <param name="side">The side (buy/sell) of the order.</param>
    /// <param name="qty">The quantity to value.</param>
    /// <param name="price">The execution price.</param>
    /// <returns>The value, excluding fees.</returns>
    decimal GetValue(IInstrument instrument, decimal leverage, OrderSide side, decimal qty, decimal price);

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
    decimal GetQty(IInstrument instrument, decimal leverage, OrderSide side, decimal sum, decimal price);
}

/*
- To calculate order result:
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static decimal ResolveResult(
        this Order order
    )
    {
        // for open order result is leveraged expense sum
        if (order.Side == order.Container.Orientation.OpenSide)
            return -order.LeveragedExpenseSum();

        var position = order.Position;
        var openedPrice = position.OpenedPrice;
        var openedValue = order.ExecutedQty * openedPrice * position.LeveragedPart();
        var priceDiff = order.Container.Orientation == Orientation.Long
            ? order.ExecutedPrice - openedPrice
            : openedPrice - order.ExecutedPrice;
        var pnl = order.ExecutedQty * priceDiff;

        return openedValue + pnl - order.Fee;
    }

- Diffing in Container - Position contains sum field (but this may be OK, cause field is a read only one and might make more sense in this view (qty * price))
        static (decimal qty, decimal activeQty, decimal sum, decimal fee) GetDiff(Order order, decimal prevExecutedQty, decimal prevFee)
        {
            var qty = order.ExecutedQty - prevExecutedQty;
            var activeQty = -qty;
            var sum = qty * order.ExecutedPrice;
            var fee = order.Fee - prevFee;

            return (qty, activeQty, sum, fee);
        }

- In fee calculation in tests:
    public static void AssertFilled(
        this Order order,
        decimal price
    )
    {
        order.Status.Is(OrderStatus.Filled);
        order.ExecutedQty.Is(order.TotalQty);
        order.ExecutedPrice.Is(price);
        // TODO: finance
        order.Fee.Is((order.ExecutedQty * order.ExecutedPrice).Fee());
    }

- Calculate qty to order from available asset and percentage of it (main - qty is calculated, not order sum)
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static decimal SizeToQty(this IStrategyContext ctx, decimal size, decimal price) =>
        ctx.CurrentValue * size / price;


- Verify whether order can be initiated
    public bool CanInit(Order order)
    {
        // TODO: in ideal, need to keep fee in mind
        var sum = order.LeveragedTotalValue();

        return sum <= _balance.Free;
    }

- In CommandExecutor - to manipulate balance value and ensure balance is enough to open order
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ExecuteOpen(
        IOpenOrderCommand command,
        RunConfig cfg,
        IManagedStrategyBalance balance
    )
    {
        var now = _timeProvider.Now;
        var order = command.Order;

        var sum = order.LeveragedTotalValue().PlusFee(order.Position.Leverage);
        if (sum > balance.Free)
        {
            if (!cfg.AllowEmptyBalance)
                throw new InvalidOperationException($"Free balance {balance.Free} is not enough to execute {command}");

            // this.Trace($"free balance {balance.Free} is not enough to execute {command}, order will be skipped");
            order.Update(OrderStatus.Canceled, 0, 0, 0, OrderCancelReason.EmptyBalance, now);
        }
        else
            order.Update(OrderStatus.New, 0, 0, 0, OrderCancelReason.None, now);

        if (order.Status is OrderStatus.New)
        {
            // this.Trace($"{order.Container.Position.Instrument}: added {order} with frozen sum {sum}");
            balance.Freeze(sum);
        }
        else
        {
            // this.Trace($"{order.Container.Position.Instrument}: skipped {order}");
        }
    }

- in StrategyCalculator to calculate current value / sum to lock on balance
    public void SyncBalance()
    {
        var toLock = _calculator.GetSumToLock(_readOnlyInstruments);
        var locked = Balance.Locked;

        if (toLock > locked)
            Balance.Lock(toLock - locked);
        else if (locked > toLock)
            Balance.Unlock(locked - toLock);
    }

    public (decimal openValue, decimal currentValue) GetValue(
        IReadOnlyDictionary<Guid, IInstrumentContext> instruments
    )
    {
        var openValue = 0m;
        var currentValue = 0m;

        foreach (var c in instruments.Values)
        {
            var position = c.Position;
            if (!position.HasOrientation)
                continue;

            var ticker = c.Ticker;
            var price = position.Orientation == Orientation.Long ? ticker.BidPrice : ticker.AskPrice;

            var positionOpen = position.OpenCost();
            var positionCurrent = position.Value(price);
            // this.Trace($"{position}: open: {positionOpen}, current: {positionCurrent}");
            openValue += positionOpen;
            currentValue += positionCurrent;
        }

        return (openValue, currentValue);
    }

    public decimal GetSumToLock(
        IReadOnlyDictionary<Guid, IInstrumentContext> instruments
    )
    {
        var locked = 0m;
        foreach (var c in instruments.Values)
            locked += GetSumToLock(c);

        return locked;
    }

    [Fact]
    public void GetValue_Long()
    {
        // arrange
        var calc = Get<IStrategyCalculator>();

        // act - no orientation
        var (openValue, currentValue) = calc.GetValue(_instruments);

        // assert - no orientation
        openValue.Is(0);
        currentValue.Is(0);

        // act - with orientation
        var position = _instrument.Position;
        position.SetOrientation(Orientation.Long);
        var order = position.AddContainer(Orientation.Long, 10).AddLimitOpenOrder(20, 10).Fill();
        _instrument.SetTicker(new InstrumentTicker(_instrument.Instrument.Id, 9, 11));
        (openValue, currentValue) = calc.GetValue(_instruments);

        // assert - with orientation
        openValue.Is(order.LeveragedExpenseSum());
        currentValue.Is(280 - 200m.PlusFee());
    }
*/
