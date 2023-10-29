using Annium.Finance.Providers.Abstractions.Domain.Interfaces;
using Annium.Finance.Providers.Abstractions.Domain.Operations;

namespace Annium.Finance.Providers.Crypto.Binance.Base.Internal.Contracts.User.Modules;

internal static class Validation
{
    private static readonly UserResult EmptySymbol = UserResult.New(UserOperationStatus.BadRequest, "empty symbol");
    private static readonly UserResult InvalidQuantity = UserResult.New(
        UserOperationStatus.BadRequest,
        "invalid quantity"
    );
    private static readonly UserResult InvalidPrice = UserResult.New(UserOperationStatus.BadRequest, "invalid price");
    private static readonly UserResult InvalidTriggerPrice = UserResult.New(
        UserOperationStatus.BadRequest,
        "invalid trigger price"
    );

    public static UserResult? Symbol(IInitOrderRequest request)
    {
        return !string.IsNullOrWhiteSpace(request.Symbol) ? null : EmptySymbol;
    }

    public static UserResult? PositiveQuantity(IInitOrderRequest request)
    {
        return request.Qty > 0 ? null : InvalidQuantity;
    }

    public static UserResult? PositiveOrZeroQuantity(IInitOrderRequest request)
    {
        return request.Qty >= 0 ? null : InvalidQuantity;
    }

    public static UserResult? PositivePrice(IInitOrderRequest request)
    {
        return request.Price > 0 ? null : InvalidPrice;
    }

    public static UserResult? ZeroPrice(IInitOrderRequest request)
    {
        return request.Price == 0 ? null : InvalidPrice;
    }

    public static UserResult? PositiveTriggerPrice(IInitOrderRequest request)
    {
        return request.LevelPrice > 0 ? null : InvalidTriggerPrice;
    }

    public static UserResult? ZeroTriggerPrice(IInitOrderRequest request)
    {
        return request.LevelPrice == 0 ? null : InvalidTriggerPrice;
    }
}
