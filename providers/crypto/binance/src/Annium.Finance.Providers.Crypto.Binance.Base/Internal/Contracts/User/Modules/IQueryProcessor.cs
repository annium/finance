using System.Collections.Generic;
using Annium.Finance.Providers.Abstractions.Domain.Dto;
using Annium.Finance.Providers.Abstractions.Domain.Interfaces;
using Annium.Finance.Providers.Abstractions.Domain.Operations;

namespace Annium.Finance.Providers.Crypto.Binance.Base.Internal.Contracts.User.Modules;

public interface IQueryProcessor
{
    Result<UserOperationStatus, Dictionary<string, string>> BuildInitOrderQuery(IInitOrderRequest request);
    Result<UserOperationStatus, Dictionary<string, string>> BuildModifyOrderQuery(IModifyOrderRequest request);
    Result<UserOperationStatus, Dictionary<string, string>> BuildCancelOrderQuery(OrderDto order);
    Result<UserOperationStatus, Dictionary<string, string>> BuildCancelAllOrdersQuery(string symbol);
}
