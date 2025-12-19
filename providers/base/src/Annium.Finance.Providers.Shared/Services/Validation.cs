using Annium.Finance.Providers.Abstractions.Domain.User.Operations;
using Annium.Finance.Providers.Abstractions.Domain.User.Requests;

namespace Annium.Finance.Providers.Shared.Services;

public static class Validation
{
    private static readonly UserResult _emptySymbol = UserResult.New(UserOperationStatus.BadRequest, "empty symbol");
    private static readonly UserResult _invalidQuantity = UserResult.New(
        UserOperationStatus.BadRequest,
        "invalid quantity"
    );
    private static readonly UserResult _invalidPrice = UserResult.New(UserOperationStatus.BadRequest, "invalid price");
    private static readonly UserResult _invalidTriggerPrice = UserResult.New(
        UserOperationStatus.BadRequest,
        "invalid trigger price"
    );

    public static UserResult? Symbol(IInitOrderRequest request)
    {
        return !string.IsNullOrWhiteSpace(request.Symbol) ? null : _emptySymbol;
    }

    public static UserResult? PositiveQuantity(IInitOrderRequest request)
    {
        return request.Qty > 0 ? null : _invalidQuantity;
    }

    public static UserResult? PositiveOrZeroQuantity(IInitOrderRequest request)
    {
        return request.Qty >= 0 ? null : _invalidQuantity;
    }

    public static UserResult? PositivePrice(IInitOrderRequest request)
    {
        return request.Price > 0 ? null : _invalidPrice;
    }

    public static UserResult? ZeroPrice(IInitOrderRequest request)
    {
        return request.Price == 0 ? null : _invalidPrice;
    }

    public static UserResult? PositiveTriggerPrice(IInitOrderRequest request)
    {
        return request.LevelPrice > 0 ? null : _invalidTriggerPrice;
    }

    public static UserResult? ZeroTriggerPrice(IInitOrderRequest request)
    {
        return request.LevelPrice == 0 ? null : _invalidTriggerPrice;
    }
}
