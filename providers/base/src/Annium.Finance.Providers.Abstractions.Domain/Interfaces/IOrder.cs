using Annium.Finance.Providers.Abstractions.Domain.Enums;

namespace Annium.Finance.Providers.Abstractions.Domain.Interfaces;

public interface IOrder
{
    OrderSide Side { get; }
    OrderType Type { get; }
    decimal TotalQty { get; }
    decimal Price { get; }
    decimal LevelPrice { get; }
    OrderStatus Status { get; }
    decimal ExecutedQty { get; }
    decimal ExecutedPrice { get; }
}
