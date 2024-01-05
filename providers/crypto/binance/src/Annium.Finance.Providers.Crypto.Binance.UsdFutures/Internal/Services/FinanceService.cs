using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Connectors.Services;
using Annium.Finance.Providers.Abstractions.Domain.Enums;
using Annium.Finance.Providers.Abstractions.Domain.Interfaces;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Services;

internal class FinanceService : IFinanceService
{
    public ValueTask InitAsync(ProviderEnvironment env)
    {
        return ValueTask.CompletedTask;
    }

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

    public decimal GetCost(IInstrument instrument, decimal leverage, OrderSide side, decimal qty, decimal price)
    {
        return qty * price / leverage;
    }

    public decimal GetBorrowedSum(IInstrument instrument, decimal leverage, OrderSide side, decimal qty, decimal price)
    {
        if (leverage == 0)
            return 0;

        return qty * price * (leverage - 1) / leverage;
    }

    public decimal GetValue(IInstrument instrument, decimal leverage, OrderSide side, decimal qty, decimal price)
    {
        return qty * price / leverage;
    }

    public decimal GetQty(IInstrument instrument, decimal leverage, OrderSide side, decimal sum, decimal price)
    {
        return sum * leverage / price;
    }
}
