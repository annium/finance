using Annium.Finance.Providers.Abstractions.Domain.User.Operations;
using Annium.Finance.Providers.Abstractions.Domain.User.Requests;

namespace Annium.Finance.Providers.Core.User;

/// <summary>
/// Reusable validation checks for <see cref="IInitOrderRequest"/>s. Each check returns <see langword="null"/> if
/// the request is valid, or a bad-request <see cref="UserResult"/> describing the failure otherwise, so checks
/// can be composed and the first non-null result returned to the caller.
/// </summary>
public static class Validation
{
    /// <summary>The failure result for a request with an empty symbol.</summary>
    private static readonly UserResult _emptySymbol = UserResult.New(UserOperationStatus.BadRequest, "empty symbol");

    /// <summary>The failure result for a request with an invalid quantity.</summary>
    private static readonly UserResult _invalidQuantity = UserResult.New(
        UserOperationStatus.BadRequest,
        "invalid quantity"
    );

    /// <summary>The failure result for a request with an invalid price.</summary>
    private static readonly UserResult _invalidPrice = UserResult.New(UserOperationStatus.BadRequest, "invalid price");

    /// <summary>The failure result for a request with an invalid trigger price.</summary>
    private static readonly UserResult _invalidTriggerPrice = UserResult.New(
        UserOperationStatus.BadRequest,
        "invalid trigger price"
    );

    /// <summary>
    /// Checks that the request's symbol is set.
    /// </summary>
    /// <param name="request">The request to validate.</param>
    /// <returns>A bad-request result if the symbol is empty or whitespace; otherwise, <see langword="null"/>.</returns>
    public static UserResult? Symbol(IInitOrderRequest request)
    {
        return !string.IsNullOrWhiteSpace(request.Symbol) ? null : _emptySymbol;
    }

    /// <summary>
    /// Checks that the request's quantity is strictly positive.
    /// </summary>
    /// <param name="request">The request to validate.</param>
    /// <returns>A bad-request result if the quantity is not positive; otherwise, <see langword="null"/>.</returns>
    public static UserResult? PositiveQuantity(IInitOrderRequest request)
    {
        return request.Qty > 0 ? null : _invalidQuantity;
    }

    /// <summary>
    /// Checks that the request's quantity is zero or positive.
    /// </summary>
    /// <param name="request">The request to validate.</param>
    /// <returns>A bad-request result if the quantity is negative; otherwise, <see langword="null"/>.</returns>
    public static UserResult? PositiveOrZeroQuantity(IInitOrderRequest request)
    {
        return request.Qty >= 0 ? null : _invalidQuantity;
    }

    /// <summary>
    /// Checks that the request's price is strictly positive.
    /// </summary>
    /// <param name="request">The request to validate.</param>
    /// <returns>A bad-request result if the price is not positive; otherwise, <see langword="null"/>.</returns>
    public static UserResult? PositivePrice(IInitOrderRequest request)
    {
        return request.Price > 0 ? null : _invalidPrice;
    }

    /// <summary>
    /// Checks that the request's price is exactly zero.
    /// </summary>
    /// <param name="request">The request to validate.</param>
    /// <returns>A bad-request result if the price is non-zero; otherwise, <see langword="null"/>.</returns>
    public static UserResult? ZeroPrice(IInitOrderRequest request)
    {
        return request.Price == 0 ? null : _invalidPrice;
    }

    /// <summary>
    /// Checks that the request's trigger price is strictly positive.
    /// </summary>
    /// <param name="request">The request to validate.</param>
    /// <returns>A bad-request result if the trigger price is not positive; otherwise, <see langword="null"/>.</returns>
    public static UserResult? PositiveTriggerPrice(IInitOrderRequest request)
    {
        return request.LevelPrice > 0 ? null : _invalidTriggerPrice;
    }

    /// <summary>
    /// Checks that the request's trigger price is exactly zero.
    /// </summary>
    /// <param name="request">The request to validate.</param>
    /// <returns>A bad-request result if the trigger price is non-zero; otherwise, <see langword="null"/>.</returns>
    public static UserResult? ZeroTriggerPrice(IInitOrderRequest request)
    {
        return request.LevelPrice == 0 ? null : _invalidTriggerPrice;
    }
}
