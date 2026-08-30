using Annium.Finance.Providers.Abstractions.Domain.User;
using Annium.Finance.Providers.Abstractions.Domain.User.Requests;
using Annium.Testing;

namespace Annium.Finance.Providers.Tests.Lib.User;

/// <summary>
/// Asserts that an <see cref="OrderModel"/> reported by a provider matches the request that created/modified
/// it, or matches another previously reported <see cref="OrderModel"/>. Checks only the order's terms
/// (symbol, side, type, quantity, prices) - not its lifecycle status or fill state.
/// </summary>
public static class OrderModelTestExtensions
{
    /// <summary>
    /// Asserts that the order's terms match the request that placed it.
    /// </summary>
    /// <param name="order">The order reported by the provider.</param>
    /// <param name="request">The request the order was placed from.</param>
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

    /// <summary>
    /// Asserts that the order's terms match the request that modified it.
    /// </summary>
    /// <param name="order">The order reported by the provider.</param>
    /// <param name="request">The request the order was modified from.</param>
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

    /// <summary>
    /// Asserts that the order's identity and terms match a previously reported order.
    /// </summary>
    /// <param name="order">The order reported by the provider.</param>
    /// <param name="original">The original order to compare against.</param>
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
