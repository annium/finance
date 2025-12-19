using Annium.Finance.Providers.Abstractions.Domain.User;
using Annium.Finance.Providers.Abstractions.Domain.User.Requests;
using Annium.Testing;

namespace Annium.Finance.Providers.Tests.Lib.User;

public static class OrderModelTestExtensions
{
    public static void ShouldMatch(this OrderModel order, IInitOrderRequest request)
    {
        order.ClientOrderId.IsNotDefault();
        order.Id.IsNullOrWhiteSpace().IsFalse();
        order.Symbol.Is(request.Symbol);
        order.Side.Is(request.Side);
        order.Type.Is(request.Type);
        order.TotalQty.Is(request.Qty);
        order.Price.Is(request.Price);
        order.LevelPrice.Is(request.LevelPrice);
    }

    public static void ShouldMatch(this OrderModel order, IModifyOrderRequest request)
    {
        order.ClientOrderId.IsNotDefault();
        order.Id.IsNullOrWhiteSpace().IsFalse();
        order.Symbol.Is(request.Order.Symbol);
        order.Side.Is(request.Side);
        order.Type.Is(request.Type);
        order.TotalQty.Is(request.Qty);
        order.Price.Is(request.Price);
        order.LevelPrice.Is(request.LevelPrice);
    }

    public static void ShouldMatch(this OrderModel order, OrderModel original)
    {
        order.ClientOrderId.Is(original.ClientOrderId);
        order.Id.Is(original.Id);
        order.Symbol.Is(original.Symbol);
        order.Side.Is(original.Side);
        order.Type.Is(original.Type);
        order.TotalQty.Is(original.TotalQty);
        order.Price.Is(original.Price);
        order.LevelPrice.Is(original.LevelPrice);
    }
}
