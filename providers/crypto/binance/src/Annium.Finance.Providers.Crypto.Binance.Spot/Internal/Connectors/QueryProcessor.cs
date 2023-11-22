using System.Collections.Generic;
using Annium.Finance.Providers.Abstractions.Domain.Dto;
using Annium.Finance.Providers.Abstractions.Domain.Enums;
using Annium.Finance.Providers.Abstractions.Domain.Interfaces;
using Annium.Finance.Providers.Abstractions.Domain.Operations;
using Annium.Finance.Providers.Crypto.Binance.Base.Contracts.User.Modules;
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
        result["newClientOrderId"] = request.Id.ToString();
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

        result["cancelOrigClientOrderId"] = request.Order.Id.ToString();
        result["newClientOrderId"] = request.Order.Id.ToString();

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

    public UserResult<Dictionary<string, string>> BuildCancelOrderQuery(OrderDto order)
    {
        var result = new Dictionary<string, string>();

        result["origClientOrderId"] = order.Id.ToString();
        result["newClientOrderId"] = order.Id.ToString();
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
