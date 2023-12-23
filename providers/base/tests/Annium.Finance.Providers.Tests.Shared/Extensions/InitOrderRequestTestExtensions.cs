using Annium.Finance.Providers.Abstractions.Domain.Dto;
using Annium.Finance.Providers.Abstractions.Domain.Enums;
using Annium.Finance.Providers.Abstractions.Domain.Interfaces;

namespace Annium.Finance.Providers.Tests.Shared.Extensions;

public static class InitOrderRequestTestExtensions
{
    public static OrderDto ToOrder(this IInitOrderRequest request)
    {
        return new OrderDto(
            string.Empty,
            request.Id,
            request.Symbol,
            request.Side,
            request.Type,
            request.Qty,
            request.Price,
            request.LevelPrice,
            0L,
            OrderStatus.New,
            0m,
            0m,
            0m,
            0L
        );
    }
}
