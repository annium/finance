using Annium.Finance.Providers.Abstractions.Domain.User;
using Annium.Finance.Providers.Abstractions.Domain.User.Requests;

namespace Annium.Finance.Providers.Tests.Lib.User.Requests;

/// <summary>
/// Builds the <see cref="OrderModel"/> a provider would be expected to report right after accepting an
/// order-init request, before any fill has happened.
/// </summary>
public static class InitOrderRequestTestExtensions
{
    /// <summary>
    /// Builds a new, unfilled order model from an order-init request.
    /// </summary>
    /// <param name="request">The request the order was placed from.</param>
    /// <returns>A new order model in the <see cref="OrderStatus.New"/> status with no fills yet.</returns>
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
