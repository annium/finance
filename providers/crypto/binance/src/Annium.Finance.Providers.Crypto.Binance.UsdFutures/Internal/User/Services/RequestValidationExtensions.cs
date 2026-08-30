using Annium.Finance.Providers.Abstractions.Domain.User;
using Annium.Finance.Providers.Abstractions.Domain.User.Operations;
using Annium.Finance.Providers.Abstractions.Domain.User.Requests;
using static Annium.Finance.Providers.Core.User.Validation;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.User.Services;

/// <summary>
/// Validates order placement requests against the shape Binance USD-M futures expects for each order type
/// (which fields must be positive, zero, or unset).
/// </summary>
internal static class RequestValidationExtensions
{
    /// <summary>
    /// Validates the request against the field constraints of its order type: limit orders require a positive
    /// price and quantity and no trigger price, market orders require a positive quantity and no price or
    /// trigger price, and stop/take-profit orders require a positive trigger price with a price only for their
    /// limit variants.
    /// </summary>
    /// <param name="request">The order placement request to validate.</param>
    /// <returns>An OK result if the request is valid, or a bad-request failure describing the violation.</returns>
    public static UserResult Validate(this IInitOrderRequest request)
    {
        return request.Type switch
        {
            OrderType.Limit => Symbol(request)
                ?? PositiveQuantity(request)
                ?? PositivePrice(request)
                ?? ZeroTriggerPrice(request)
                ?? UserResult.Ok(),
            OrderType.Market => Symbol(request)
                ?? PositiveQuantity(request)
                ?? ZeroPrice(request)
                ?? ZeroTriggerPrice(request)
                ?? UserResult.Ok(),
            OrderType.StopLossMarket => Symbol(request)
                ?? PositiveOrZeroQuantity(request)
                ?? ZeroPrice(request)
                ?? PositiveTriggerPrice(request)
                ?? UserResult.Ok(),
            OrderType.TakeProfitMarket => Symbol(request)
                ?? PositiveOrZeroQuantity(request)
                ?? ZeroPrice(request)
                ?? PositiveTriggerPrice(request)
                ?? UserResult.Ok(),
            OrderType.StopLossLimit => Symbol(request)
                ?? PositiveQuantity(request)
                ?? PositivePrice(request)
                ?? PositiveTriggerPrice(request)
                ?? UserResult.Ok(),
            OrderType.TakeProfitLimit => Symbol(request)
                ?? PositiveQuantity(request)
                ?? PositivePrice(request)
                ?? PositiveTriggerPrice(request)
                ?? UserResult.Ok(),
            _ => UserResult.New(UserOperationStatus.BadRequest, $"Unexpected order type: {request.Type}"),
        };
    }
}
