using Annium.Finance.Providers.Abstractions.Domain.Interfaces;
using Annium.Finance.Providers.Abstractions.Domain.Internal.Models;

namespace Annium.Finance.Providers.Abstractions.Domain.Extensions;

public static class OrderRequestExtensions
{
    public static IInitOrderRequest ToInitOrderRequest(this IModifyOrderRequest request)
    {
        return new InitOrderRequest
        {
            Id = request.Order.Id,
            Symbol = request.Order.Symbol,
            Side = request.Side,
            Type = request.Type,
            Qty = request.Qty,
            Price = request.Price,
            LevelPrice = request.LevelPrice,
        };
    }
}
