using Annium.Finance.Providers.Abstractions.Domain.User.Requests;

namespace Annium.Finance.Providers.Abstractions.Domain.Internal.User.Requests;

/// <summary>
/// Default implementation of <see cref="ICancelOrderRequest"/>, built by <see cref="RequestBuilder"/>.
/// </summary>
internal sealed record CancelOrderRequest : ICancelOrderRequest
{
    /// <summary>Gets the provider-assigned identifier of the order to cancel.</summary>
    public required string Id { get; init; }

    /// <summary>Gets the client-assigned identifier of the order to cancel.</summary>
    public required string ClientOrderId { get; init; }

    /// <summary>Gets the instrument symbol the order to cancel belongs to.</summary>
    public required string Symbol { get; init; }
}
