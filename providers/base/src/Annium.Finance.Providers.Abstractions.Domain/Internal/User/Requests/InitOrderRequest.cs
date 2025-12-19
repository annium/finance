using Annium.Finance.Providers.Abstractions.Domain.User;
using Annium.Finance.Providers.Abstractions.Domain.User.Requests;

namespace Annium.Finance.Providers.Abstractions.Domain.Internal.User.Requests;

internal sealed record InitOrderRequest : IInitOrderRequest
{
    public required string Id { get; init; }
    public required OrientationRange Range { get; init; }
    public required string Symbol { get; init; }
    public required OrderSide Side { get; init; }
    public required OrderType Type { get; init; }
    public required decimal Qty { get; init; }
    public required decimal Price { get; init; }
    public required decimal LevelPrice { get; init; }
    public required bool ReduceOnly { get; init; }
}
