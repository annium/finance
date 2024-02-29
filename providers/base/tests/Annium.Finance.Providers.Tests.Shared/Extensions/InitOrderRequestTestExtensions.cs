using Annium.Finance.Providers.Abstractions.Domain.Enums;
using Annium.Finance.Providers.Abstractions.Domain.Interfaces;
using Annium.Finance.Providers.Abstractions.Domain.Models;

namespace Annium.Finance.Providers.Tests.Shared.Extensions;

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
