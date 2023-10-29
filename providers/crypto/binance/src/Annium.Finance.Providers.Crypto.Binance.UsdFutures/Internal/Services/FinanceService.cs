using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Connectors.Services;
using Annium.Finance.Providers.Abstractions.Domain.Dto;
using Annium.Finance.Providers.Abstractions.Domain.Enums;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Services;

internal class FinanceService : IFinanceService
{
    public ValueTask InitAsync(ProviderEnvironment env)
    {
        throw new System.NotImplementedException();
    }

    public ValueTask DisposeAsync()
    {
        throw new System.NotImplementedException();
    }

    public decimal GetResult(
        InstrumentDto instrument,
        Orientation orientation,
        byte leverage,
        decimal positionPrice,
        OrderSide side,
        decimal qty,
        decimal price,
        decimal fee
    )
    {
        throw new System.NotImplementedException();
    }

    public decimal GetCost(InstrumentDto instrument, byte leverage, OrderSide side, decimal qty, decimal price)
    {
        throw new System.NotImplementedException();
    }

    public decimal GetBorrowedSum(InstrumentDto instrument, byte leverage, OrderSide side, decimal qty, decimal price)
    {
        throw new System.NotImplementedException();
    }

    public decimal GetValue(InstrumentDto instrument, byte leverage, OrderSide side, decimal qty, decimal price)
    {
        throw new System.NotImplementedException();
    }

    public decimal GetQty(InstrumentDto instrument, byte leverage, OrderSide side, decimal sum, decimal price)
    {
        throw new System.NotImplementedException();
    }
}
