using Annium.Finance.Providers.Abstractions.Domain.User.Requests;

namespace Annium.Finance.Providers.Abstractions.Domain.Internal.User.Requests;

internal sealed record CancelOrderRequest : ICancelOrderRequest
{
    public required string Id { get; init; }
    public required string ClientOrderId { get; init; }
    public required string Symbol { get; init; }
}
