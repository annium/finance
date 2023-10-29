using System.Collections.Generic;
using Annium.Finance.Providers.Abstractions.Domain.Dto;
using Annium.Finance.Providers.Abstractions.Domain.Interfaces;
using Annium.Finance.Providers.Abstractions.Domain.Operations;

namespace Annium.Finance.Providers.Crypto.Binance.Base.Internal.Contracts.User.Modules;

internal interface IQueryProcessor
{
    UserResult<Dictionary<string, string>> BuildInitOrderQuery(IInitOrderRequest request);
    UserResult<Dictionary<string, string>> BuildModifyOrderQuery(IModifyOrderRequest request);
    UserResult<Dictionary<string, string>> BuildCancelOrderQuery(OrderDto order);
    UserResult<Dictionary<string, string>> BuildCancelAllOrdersQuery(string symbol);
}
