using System;
using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Connectors.Services;
using Annium.Finance.Providers.Abstractions.Domain.Enums;
using Annium.Finance.Providers.Abstractions.Domain.Models;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Internal.Services;

internal class FinanceService : IFinanceService
{
    public ValueTask InitAsync(ProviderEnvironment env)
    {
        throw new NotImplementedException();
    }

    public ValueTask DisposeAsync()
    {
        throw new NotImplementedException();
    }

    public decimal GetResult(
        InstrumentModel instrument,
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

    public decimal GetCost(InstrumentModel instrument, byte leverage, OrderSide side, decimal qty, decimal price)
    {
        throw new NotImplementedException();
    }

    public decimal GetBorrowedSum(InstrumentModel instrument, byte leverage, OrderSide side, decimal qty, decimal price)
    {
        throw new NotImplementedException();
    }

    public decimal GetValue(InstrumentModel instrument, byte leverage, OrderSide side, decimal qty, decimal price)
    {
        throw new NotImplementedException();
    }

    public decimal GetQty(InstrumentModel instrument, byte leverage, OrderSide side, decimal sum, decimal price)
    {
        throw new NotImplementedException();
    }
}
