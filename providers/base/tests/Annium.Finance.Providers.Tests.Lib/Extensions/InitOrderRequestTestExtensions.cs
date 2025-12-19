using Annium.Finance.Providers.Abstractions.Domain.User;
using Annium.Finance.Providers.Abstractions.Domain.User.Requests;

namespace Annium.Finance.Providers.Tests.Lib.Extensions;

public static class InitOrderRequestTestExtensions
{
    public static OrderModel ToOrder(this IInitOrderRequest request)
    {
        return new OrderModel(
            string.Empty,
            request.Id,
            OrientationRange.Both,
            request.Symbol,
            request.Side,
            request.Type,
            request.Qty,
            request.Price,
            request.LevelPrice,
            request.ReduceOnly,
            0L,
            OrderStatus.New,
            0m,
            0m,
            0L
        );
    }
}
