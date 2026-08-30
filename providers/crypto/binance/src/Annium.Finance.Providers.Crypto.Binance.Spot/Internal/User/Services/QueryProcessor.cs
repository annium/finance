using System.Collections.Generic;
using Annium.Finance.Providers.Abstractions.Domain.User;
using Annium.Finance.Providers.Abstractions.Domain.User.Operations;
using Annium.Finance.Providers.Abstractions.Domain.User.Requests;
using Annium.Finance.Providers.Crypto.Binance.Spot.Internal.User.Contracts.Domain;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Internal.User.Services;

/// <summary>Builds the Binance request parameters for order placement, modification and cancellation from the library's request models.</summary>
internal class QueryProcessor
{
    /// <summary>Validates and builds the request parameters for placing a new order.</summary>
    /// <param name="request">The order placement request.</param>
    /// <returns>A result carrying the request parameters on success, or the validation failure.</returns>
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

    /// <summary>Builds the request parameters for modifying an existing order via a Binance cancel-replace request, aborting the replace if the cancel fails.</summary>
    /// <param name="request">The order modification request, including the order being modified.</param>
    /// <returns>A result carrying the request parameters.</returns>
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

    /// <summary>Builds the request parameters for canceling an order, requiring at least the order id or the client order id.</summary>
    /// <param name="request">Identifies the order to cancel.</param>
    /// <returns>A result carrying the request parameters on success, or a bad-request failure if neither id is specified.</returns>
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

    /// <summary>Builds the request parameters for canceling all open orders on a symbol.</summary>
    /// <param name="symbol">The instrument symbol to cancel orders for.</param>
    /// <returns>A result carrying the request parameters.</returns>
    public UserResult<Dictionary<string, string>> BuildCancelAllOrdersQuery(string symbol)
    {
        var result = new Dictionary<string, string>();

        result["symbol"] = symbol;

        return UserResult.Ok(result);
    }
}
