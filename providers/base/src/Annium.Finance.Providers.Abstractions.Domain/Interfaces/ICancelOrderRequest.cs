namespace Annium.Finance.Providers.Abstractions.Domain.Interfaces;

public interface ICancelOrderRequest
{
    string Id { get; }
    string ClientOrderId { get; }
    string Symbol { get; }
}
