using Annium.Finance.Providers.Abstractions.Domain.User;
using Annium.Finance.Providers.Abstractions.Domain.User.Operations;
using Annium.Finance.Providers.Abstractions.Domain.User.Requests;
using static Annium.Finance.Providers.Core.User.Validation;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.User.Services;

internal static class RequestValidationExtensions
{
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
