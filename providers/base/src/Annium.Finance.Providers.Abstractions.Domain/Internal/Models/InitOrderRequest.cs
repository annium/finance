using Annium.Finance.Providers.Abstractions.Domain.Enums;
using Annium.Finance.Providers.Abstractions.Domain.Interfaces;

namespace Annium.Finance.Providers.Abstractions.Domain.Internal.Models;

internal sealed record InitOrderRequest : IInitOrderRequest
{
    public required string Id { get; init; }
    public required string Symbol { get; init; }
    public required OrderSide Side { get; init; }
    public required OrderType Type { get; init; }
    public required decimal Qty { get; init; }
    public required decimal Price { get; init; }
    public required decimal LevelPrice { get; init; }
    public required bool ReduceOnly { get; init; }
}
