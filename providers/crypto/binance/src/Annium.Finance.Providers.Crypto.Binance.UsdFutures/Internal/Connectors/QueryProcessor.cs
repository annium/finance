using System.Collections.Generic;
using Annium.Finance.Providers.Abstractions.Connectors.Connectors;
using Annium.Finance.Providers.Abstractions.Domain.Enums;
using Annium.Finance.Providers.Abstractions.Domain.Interfaces;
using Annium.Finance.Providers.Abstractions.Domain.Models;
using Annium.Finance.Providers.Abstractions.Domain.Operations;
using Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Contracts.User.Domain;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Connectors;

internal class QueryProcessor : IQueryProcessor
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
        result["positionSide"] = "BOTH";
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
                SetQtyOrClosePosition(request, result);
                result["stopPrice"] = request.LevelPrice.ToGeneralInvariantString();
                break;
            case OrderType.TakeProfitMarket:
                SetQtyOrClosePosition(request, result);
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

        static void SetQtyOrClosePosition(IInitOrderRequest request, IDictionary<string, string> result)
        {
            if (request.Qty == 0)
            {
                result["closePosition"] = "true";
            }
            else
            {
                result["quantity"] = request.Qty.ToGeneralInvariantString();
                TrySetReduceOnly(request, result);
            }
        }

        static void TrySetReduceOnly(IInitOrderRequest request, IDictionary<string, string> result)
        {
            if (request.ReduceOnly)
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

    public UserResult<Dictionary<string, string>> BuildCancelOrderQuery(OrderModel order)
    {
        var result = new Dictionary<string, string>();

        result["origClientOrderId"] = order.ClientOrderId;
        result["newClientOrderId"] = order.ClientOrderId;
        result["symbol"] = order.Symbol;

        return UserResult.Ok(result);
    }

    public UserResult<Dictionary<string, string>> BuildCancelAllOrdersQuery(string symbol)
    {
        var result = new Dictionary<string, string>();

        result["symbol"] = symbol;

        return UserResult.Ok(result);
    }
}
