namespace Annium.Finance.Providers.Abstractions.Domain.User.Requests;

public interface IModifyOrderRequest
{
    OrderModel Order { get; }
    OrderSide Side { get; }
    OrderType Type { get; }
    decimal Qty { get; }
    decimal Price { get; }
    decimal LevelPrice { get; }
}
