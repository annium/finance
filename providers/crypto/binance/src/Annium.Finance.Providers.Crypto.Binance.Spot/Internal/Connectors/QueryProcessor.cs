using System.Collections.Generic;
using Annium.Finance.Providers.Abstractions.Connectors.Connectors;
using Annium.Finance.Providers.Abstractions.Domain.Enums;
using Annium.Finance.Providers.Abstractions.Domain.Interfaces;
using Annium.Finance.Providers.Abstractions.Domain.Operations;
using Annium.Finance.Providers.Crypto.Binance.Spot.Internal.Contracts.User.Domain;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Internal.Connectors;

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
        result["type"] = OrderTypes.ValueToString[request.Type];
        result["newOrderRespType"] = "RESULT";

        switch (request.Type)
        {
            case OrderType.Limit:
                result["timeInForce"] = "GTC";
                result["quantity"] = request.Qty.ToGeneralInvariantString();
                result["price"] = request.Price.ToGeneralInvariantString();
                break;
            case OrderType.Market:
                result["quantity"] = request.Qty.ToGeneralInvariantString();
                break;
            case OrderType.StopLossMarket:
                result["quantity"] = request.Qty.ToGeneralInvariantString();
                result["stopPrice"] = request.LevelPrice.ToGeneralInvariantString();
                break;
            case OrderType.TakeProfitMarket:
                result["quantity"] = request.Qty.ToGeneralInvariantString();
                result["stopPrice"] = request.LevelPrice.ToGeneralInvariantString();
                break;
            case OrderType.StopLossLimit:
                result["timeInForce"] = "GTC";
                result["quantity"] = request.Qty.ToGeneralInvariantString();
                result["price"] = request.Price.ToGeneralInvariantString();
                result["stopPrice"] = request.LevelPrice.ToGeneralInvariantString();
                break;
            case OrderType.TakeProfitLimit:
                result["timeInForce"] = "GTC";
                result["quantity"] = request.Qty.ToGeneralInvariantString();
                result["price"] = request.Price.ToGeneralInvariantString();
                result["stopPrice"] = request.LevelPrice.ToGeneralInvariantString();
                break;
        }

        return UserResult.Ok(result);
    }

    public UserResult<Dictionary<string, string>> BuildModifyOrderQuery(IModifyOrderRequest request)
    {
        var result = new Dictionary<string, string>();

        result["symbol"] = request.Order.Symbol;
        result["side"] = OrderSides.ValueToString[request.Side];
        result["type"] = OrderTypes.ValueToString[request.Type];
        result["cancelReplaceMode"] = "STOP_ON_FAILURE";
        result["timeInForce"] = "GTC";
        result["newOrderRespType"] = "RESULT";
        result["cancelOrigClientOrderId"] = request.Order.ClientOrderId;
        result["newClientOrderId"] = request.Order.ClientOrderId;

        switch (request.Type)
        {
            case OrderType.Limit:
                result["quantity"] = request.Qty.ToGeneralInvariantString();
                result["price"] = request.Price.ToGeneralInvariantString();
                break;
            case OrderType.Market:
                result["quantity"] = request.Qty.ToGeneralInvariantString();
                break;
            case OrderType.StopLossMarket:
                result["quantity"] = request.Qty.ToGeneralInvariantString();
                result["stopPrice"] = request.LevelPrice.ToGeneralInvariantString();
                break;
            case OrderType.TakeProfitMarket:
                result["quantity"] = request.Qty.ToGeneralInvariantString();
                result["stopPrice"] = request.LevelPrice.ToGeneralInvariantString();
                break;
            case OrderType.StopLossLimit:
                result["quantity"] = request.Qty.ToGeneralInvariantString();
                result["price"] = request.Price.ToGeneralInvariantString();
                result["stopPrice"] = request.LevelPrice.ToGeneralInvariantString();
                break;
            case OrderType.TakeProfitLimit:
                result["quantity"] = request.Qty.ToGeneralInvariantString();
                result["price"] = request.Price.ToGeneralInvariantString();
                result["stopPrice"] = request.LevelPrice.ToGeneralInvariantString();
                break;
        }

        return UserResult.Ok(result);
    }

    public UserResult<Dictionary<string, string>> BuildCancelOrderQuery(ICancelOrderRequest request)
    {
        var result = new Dictionary<string, string>();

        if (!string.IsNullOrWhiteSpace(request.Id))
            result["orderId"] = request.Id;

        if (!string.IsNullOrWhiteSpace(request.Id))
        {
            result["origClientOrderId"] = request.ClientOrderId;
            result["newClientOrderId"] = request.ClientOrderId;
        }

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
