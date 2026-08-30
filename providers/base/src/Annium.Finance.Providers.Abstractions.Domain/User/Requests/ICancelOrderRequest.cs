namespace Annium.Finance.Providers.Abstractions.Domain.User.Requests;

/// <summary>
/// Represents a request to cancel a previously placed order.
/// </summary>
public interface ICancelOrderRequest
{
    /// <summary>Gets the provider-assigned identifier of the order to cancel.</summary>
    string Id { get; }

    /// <summary>Gets the client-assigned identifier of the order to cancel.</summary>
    string ClientOrderId { get; }

    /// <summary>Gets the instrument symbol the order to cancel belongs to.</summary>
    string Symbol { get; }
}
