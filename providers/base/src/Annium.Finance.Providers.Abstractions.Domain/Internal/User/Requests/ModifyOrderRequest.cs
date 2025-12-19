using Annium.Finance.Providers.Abstractions.Domain.User;
using Annium.Finance.Providers.Abstractions.Domain.User.Requests;

namespace Annium.Finance.Providers.Abstractions.Domain.Internal.User.Requests;

internal sealed record ModifyOrderRequest : IModifyOrderRequest
{
    public required OrderModel Order { get; init; }
    public required OrderSide Side { get; init; }
    public required OrderType Type { get; init; }
    public required decimal Qty { get; init; }
    public required decimal Price { get; init; }
    public required decimal LevelPrice { get; init; }
}
