using Annium.Finance.Providers.Abstractions.Domain.Interfaces;

namespace Annium.Finance.Providers.Abstractions.Domain.Internal.Models;

internal sealed record CancelOrderRequest : ICancelOrderRequest
{
    public required string Id { get; init; }
    public required string ClientOrderId { get; init; }
    public required string Symbol { get; init; }
}
