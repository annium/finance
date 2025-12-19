using System;
using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Connectors.User;
using Annium.Finance.Providers.Abstractions.Domain.Market;
using Annium.Finance.Providers.Abstractions.Domain.Shared;
using Annium.Finance.Providers.Abstractions.Domain.User;

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
        decimal leverage,
        decimal positionPrice,
        OrderSide side,
        decimal qty,
        decimal price
    )
    {
        throw new NotImplementedException();
    }

    public decimal GetCost(IInstrument instrument, decimal leverage, OrderSide side, decimal qty, decimal price)
    {
        throw new NotImplementedException();
    }

    public decimal GetBorrowedSum(IInstrument instrument, decimal leverage, OrderSide side, decimal qty, decimal price)
    {
        throw new NotImplementedException();
    }

    public decimal GetValue(IInstrument instrument, decimal leverage, OrderSide side, decimal qty, decimal price)
    {
        throw new NotImplementedException();
    }

    public decimal GetQty(IInstrument instrument, decimal leverage, OrderSide side, decimal sum, decimal price)
    {
        throw new NotImplementedException();
    }
}
