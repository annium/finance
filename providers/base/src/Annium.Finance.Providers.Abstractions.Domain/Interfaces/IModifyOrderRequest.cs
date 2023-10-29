using Annium.Finance.Providers.Abstractions.Domain.Dto;
using Annium.Finance.Providers.Abstractions.Domain.Enums;

namespace Annium.Finance.Providers.Abstractions.Domain.Interfaces;

public interface IModifyOrderRequest
{
    OrderDto Order { get; }
    OrderSide Side { get; }
    OrderType Type { get; }
    decimal Qty { get; }
    decimal Price { get; }
    decimal LevelPrice { get; }
}
