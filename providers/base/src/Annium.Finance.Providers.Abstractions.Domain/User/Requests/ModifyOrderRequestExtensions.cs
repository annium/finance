using Annium.Finance.Providers.Abstractions.Domain.Internal.User.Requests;

namespace Annium.Finance.Providers.Abstractions.Domain.User.Requests;

/// <summary>
/// Provides conversion helpers for <see cref="IModifyOrderRequest"/>.
/// </summary>
public static class ModifyOrderRequestExtensions
{
    /// <summary>Converts a modify request into an equivalent init request, reusing the original order's identifier, range, symbol and reduce-only flag.</summary>
    /// <param name="request">The order-modification request.</param>
    /// <returns>An <see cref="IInitOrderRequest"/> describing the same order as the requested modification.</returns>
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
