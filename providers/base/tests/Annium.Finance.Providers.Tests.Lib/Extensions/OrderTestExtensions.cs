using Annium.Data.Operations;
using Annium.Finance.Providers.Abstractions.Domain.Shared.Operations;
using Annium.Finance.Providers.Abstractions.Domain.User;
using Annium.Finance.Providers.Tests.Lib.Models;

namespace Annium.Finance.Providers.Tests.Lib.Extensions;

public static class OrderTestExtensions
{
    public static IResult<Order> AddToPosition(this Order order)
    {
        var result = order.AsResult().ValidateStatus(OrderStatus.New).ValidateCanProcess();

        order.Position.AddOrder(order.Id, order.Side, order.TotalQty, order.CreatedAt, result);

        return result;
    }
}
