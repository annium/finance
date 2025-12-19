using Annium.Finance.Providers.Abstractions.Domain.Internal.User.Requests;

namespace Annium.Finance.Providers.Abstractions.Domain.User.Requests;

public static class ModifyOrderRequestExtensions
{
    public static IInitOrderRequest ToInitOrderRequest(this IModifyOrderRequest request)
    {
        return new InitOrderRequest
        {
            Id = request.Order.ClientOrderId,
            Range = request.Order.Range,
            Symbol = request.Order.Symbol,
            Side = request.Side,
            Type = request.Type,
            Qty = request.Qty,
            Price = request.Price,
            LevelPrice = request.LevelPrice,
            ReduceOnly = request.Order.ReduceOnly,
        };
    }
}
