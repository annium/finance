using System;
using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Connectors.Services;
using Annium.Finance.Providers.Abstractions.Domain.Enums;
using Annium.Finance.Providers.Abstractions.Domain.Interfaces;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Internal.Services;

internal class FinanceService : IFinanceService
{
    public ValueTask InitAsync(ProviderEnvironment env)
    {
        throw new NotImplementedException();
    }

    public decimal GetResult(
        IInstrument instrument,
        Orientation orientation,
        byte leverage,
        decimal positionPrice,
        OrderSide side,
        decimal qty,
        decimal price
    )
    {
        throw new NotImplementedException();
    }

    public decimal GetCost(IInstrument instrument, byte leverage, OrderSide side, decimal qty, decimal price)
    {
        throw new NotImplementedException();
    }

    public decimal GetBorrowedSum(IInstrument instrument, byte leverage, OrderSide side, decimal qty, decimal price)
    {
        throw new NotImplementedException();
    }

    public decimal GetValue(IInstrument instrument, byte leverage, OrderSide side, decimal qty, decimal price)
    {
        throw new NotImplementedException();
    }

    public decimal GetQty(IInstrument instrument, byte leverage, OrderSide side, decimal sum, decimal price)
    {
        throw new NotImplementedException();
    }
}
