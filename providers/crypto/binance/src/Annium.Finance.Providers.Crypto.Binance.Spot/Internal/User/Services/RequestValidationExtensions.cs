using Annium.Finance.Providers.Abstractions.Domain.User;
using Annium.Finance.Providers.Abstractions.Domain.User.Operations;
using Annium.Finance.Providers.Abstractions.Domain.User.Requests;
using static Annium.Finance.Providers.Core.User.Validation;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Internal.User.Services;

/// <summary>Validation rules for order placement requests, specific to what each Binance order type requires.</summary>
internal static class RequestValidationExtensions
{
    /// <summary>Validates an order placement request against the field constraints of its order type (e.g. a limit order requires a price, a market order must not have one).</summary>
    /// <param name="request">The order placement request to validate.</param>
    /// <returns>A result indicating whether the request is valid.</returns>
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
                ?? PositiveQuantity(request)
                ?? ZeroPrice(request)
                ?? PositiveTriggerPrice(request)
                ?? UserResult.Ok(),
            OrderType.TakeProfitMarket => Symbol(request)
                ?? PositiveQuantity(request)
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
