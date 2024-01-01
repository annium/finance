using Annium.Finance.Providers.Abstractions.Domain.Enums;
using Annium.Finance.Providers.Abstractions.Domain.Interfaces;
using Annium.Finance.Providers.Abstractions.Domain.Models;

namespace Annium.Finance.Providers.Abstractions.Domain.Internal.Models;

internal sealed record ModifyOrderRequest : IModifyOrderRequest
{
    public required OrderModel Order { get; init; }
    public required OrderSide Side { get; init; }
    public required OrderType Type { get; init; }
    public required decimal Qty { get; init; }
    public required decimal Price { get; init; }
    public required decimal LevelPrice { get; init; }
}
