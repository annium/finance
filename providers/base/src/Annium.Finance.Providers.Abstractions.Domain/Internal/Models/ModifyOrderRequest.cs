using Annium.Finance.Providers.Abstractions.Domain.Dto;
using Annium.Finance.Providers.Abstractions.Domain.Enums;
using Annium.Finance.Providers.Abstractions.Domain.Interfaces;

namespace Annium.Finance.Providers.Abstractions.Domain.Internal.Models;

internal class ModifyOrderRequest : IModifyOrderRequest
{
    public required OrderDto Order { get; init; }
    public required OrderSide Side { get; init; }
    public required OrderType Type { get; init; }
    public required decimal Qty { get; init; }
    public required decimal Price { get; init; }
    public required decimal LevelPrice { get; init; }
}
