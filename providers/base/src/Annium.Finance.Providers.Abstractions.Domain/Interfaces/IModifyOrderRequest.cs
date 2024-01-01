using Annium.Finance.Providers.Abstractions.Domain.Enums;
using Annium.Finance.Providers.Abstractions.Domain.Models;

namespace Annium.Finance.Providers.Abstractions.Domain.Interfaces;

public interface IModifyOrderRequest
{
    OrderModel Order { get; }
    OrderSide Side { get; }
    OrderType Type { get; }
    decimal Qty { get; }
    decimal Price { get; }
    decimal LevelPrice { get; }
}
