using Annium.Finance.Providers.Abstractions.Domain.Enums;

namespace Annium.Finance.Providers.Abstractions.Domain.Interfaces;

public interface IInitOrderRequest
{
    string Id { get; }
    OrientationRange Range { get; }
    string Symbol { get; }
    OrderSide Side { get; }
    OrderType Type { get; }
    decimal Qty { get; }
    decimal Price { get; }
    decimal LevelPrice { get; }
    bool ReduceOnly { get; }
}
