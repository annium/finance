namespace Annium.Finance.Providers.Abstractions.Domain.User.Requests;

public interface ICancelOrderRequest
{
    string Id { get; }
    string ClientOrderId { get; }
    string Symbol { get; }
}
