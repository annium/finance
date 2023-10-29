using System;
using Annium.Finance.Providers.Abstractions.Domain.Enums;

namespace Annium.Finance.Providers.Abstractions.Domain.Interfaces;

public interface IInitOrderRequest
{
    Guid Id { get; }
    string Symbol { get; }
    OrderSide Side { get; }
    OrderType Type { get; }
    decimal Qty { get; }
    decimal Price { get; }
    decimal LevelPrice { get; }
}
