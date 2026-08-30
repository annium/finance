using Annium.Data.Operations;
using Annium.Finance.Providers.Abstractions.Domain.User;
using Annium.Finance.Providers.Tests.Lib.Shared.Operations;

namespace Annium.Finance.Providers.Tests.Lib.User;

/// <summary>
/// Registers a freshly created fake <see cref="Order"/> with its <see cref="Position"/>.
/// </summary>
public static class OrderTestExtensions
{
    /// <summary>
    /// Validates that the order is new and processable, and adds it to its position's tracked orders.
    /// </summary>
    /// <param name="order">The order to register with its position.</param>
    /// <returns>A result carrying the order on success, or the validation errors that blocked adding it.</returns>
    public static IResult<Order> AddToPosition(this Order order)
    {
        var result = order.AsResult().ValidateStatus(OrderStatus.New).ValidateCanProcess();

        order.Position.AddOrder(order.Id, order.Side, order.TotalQty, order.CreatedAt, result);

        return result;
    }
}
