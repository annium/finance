using System.Collections.Generic;
using Annium.Finance.Providers.Abstractions.Domain.User;
using Annium.Finance.Providers.Abstractions.Domain.User.Operations;
using Annium.Finance.Providers.Abstractions.Domain.User.Requests;
using Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.User.Contracts.Domain;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.User;

internal class QueryProcessor
{
    public UserResult<Dictionary<string, string>> BuildInitOrderQuery(IInitOrderRequest request)
    {
        var validationResult = request.Validate();
        if (validationResult.IsFailure)
        {
            return UserResult.From(validationResult, new Dictionary<string, string>());
        }

        var result = new Dictionary<string, string>();
        result["newClientOrderId"] = request.Id;
        result["symbol"] = request.Symbol;
        result["side"] = OrderSides.ValueToString[request.Side];
        result["positionSide"] = OrientationRanges.ValueToString[request.Range];
        result["type"] = OrderTypes.ValueToString[request.Type];
        result["newOrderRespType"] = "RESULT";

        switch (request.Type)
        {
            case OrderType.Limit:
                result["timeInForce"] = "GTC";
                result["quantity"] = request.Qty.ToGeneralInvariantString();
                result["price"] = request.Price.ToGeneralInvariantString();
                TrySetReduceOnly(request, result);
                break;
            case OrderType.Market:
                result["quantity"] = request.Qty.ToGeneralInvariantString();
                TrySetReduceOnly(request, result);
                break;
            case OrderType.StopLossMarket:
                result["quantity"] = request.Qty.ToGeneralInvariantString();
                TrySetReduceOnly(request, result);
                result["stopPrice"] = request.LevelPrice.ToGeneralInvariantString();
                break;
            case OrderType.TakeProfitMarket:
                result["quantity"] = request.Qty.ToGeneralInvariantString();
                TrySetReduceOnly(request, result);
                result["stopPrice"] = request.LevelPrice.ToGeneralInvariantString();
                break;
            case OrderType.StopLossLimit:
                result["timeInForce"] = "GTC";
                result["quantity"] = request.Qty.ToGeneralInvariantString();
                result["price"] = request.Price.ToGeneralInvariantString();
                result["stopPrice"] = request.LevelPrice.ToGeneralInvariantString();
                TrySetReduceOnly(request, result);
                break;
            case OrderType.TakeProfitLimit:
                result["timeInForce"] = "GTC";
                result["quantity"] = request.Qty.ToGeneralInvariantString();
                result["price"] = request.Price.ToGeneralInvariantString();
                result["stopPrice"] = request.LevelPrice.ToGeneralInvariantString();
                TrySetReduceOnly(request, result);
                break;
        }

        return UserResult.Ok(result);

        static void TrySetReduceOnly(IInitOrderRequest request, IDictionary<string, string> result)
        {
            if (request.ReduceOnly && request.Range is OrientationRange.Both)
                result["reduceOnly"] = "true";
        }
    }

    public UserResult<Dictionary<string, string>> BuildModifyOrderQuery(IModifyOrderRequest request)
    {
        var result = new Dictionary<string, string>();

        if (request.Type is not OrderType.Limit)
        {
            return UserResult.New(UserOperationStatus.BadRequest, result, "Only limit orders are supported");
        }

        result["origClientOrderId"] = request.Order.ClientOrderId;
        result["symbol"] = request.Order.Symbol;
        result["side"] = OrderSides.ValueToString[request.Side];
        result["quantity"] = request.Qty.ToGeneralInvariantString();
        result["price"] = request.Price.ToGeneralInvariantString();

        return UserResult.Ok(result);
    }

    public UserResult<Dictionary<string, string>> BuildCancelOrderQuery(ICancelOrderRequest request)
    {
        var result = new Dictionary<string, string>();

        if (!string.IsNullOrWhiteSpace(request.Id))
            result["orderId"] = request.Id;

        if (!string.IsNullOrWhiteSpace(request.ClientOrderId))
        {
            result["origClientOrderId"] = request.ClientOrderId;
            result["newClientOrderId"] = request.ClientOrderId;
        }

        if (result.Count == 0)
            return UserResult.New(
                UserOperationStatus.BadRequest,
                result,
                "Either order id or client order id must be specified"
            );

        result["symbol"] = request.Symbol;

        return UserResult.Ok(result);
    }

    public UserResult<Dictionary<string, string>> BuildCancelAllOrdersQuery(string symbol)
    {
        var result = new Dictionary<string, string>();

        result["symbol"] = symbol;

        return UserResult.Ok(result);
    }
}
