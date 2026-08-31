using Annium.Finance.Providers.Abstractions.Domain.User;
using Annium.Finance.Providers.Abstractions.Domain.User.Operations;
using Annium.Finance.Providers.Abstractions.Domain.User.Requests;
using Annium.Finance.Providers.Core.User;
using Annium.Testing;
using Xunit;
using static Annium.Finance.Providers.Abstractions.Domain.User.Requests.RequestBuilder;

namespace Annium.Finance.Providers.Core.Tests.User;

/// <summary>
/// Pins the checks a provider runs before a request leaves the process. Each order type composes its own
/// chain of these — every Binance order type builds one — so a check that admits what it names as invalid
/// sends the exchange an order it was written to refuse first, and the refusal comes back as a rejection
/// with the exchange's own wording rather than this one.
/// </summary>
/// <remarks>
/// The pairs matter more than the individual checks. <c>PositiveQuantity</c> and
/// <c>PositiveOrZeroQuantity</c> exist as separate methods because different order types need different
/// answers about zero — reduce-only and close orders legitimately carry none — and the two are one character
/// apart. The same holds for the price and trigger-price pairs. Nothing in the solution asserted any of them.
/// </remarks>
public class ValidationTests
{
    /// <summary>
    /// A quantity of zero is refused where the check is for a strictly positive one, and accepted where zero
    /// is allowed. The two differ only in that, which is the whole reason both exist.
    /// </summary>
    /// <param name="qty">The quantity to validate.</param>
    /// <param name="positiveAccepts">Whether the strictly-positive check should accept it.</param>
    /// <param name="positiveOrZeroAccepts">Whether the zero-or-positive check should accept it.</param>
    [Theory]
    [InlineData(1, true, true)]
    [InlineData(0, false, true)]
    [InlineData(-1, false, false)]
    public void QuantityChecks_DifferExactlyOnZero(int qty, bool positiveAccepts, bool positiveOrZeroAccepts)
    {
        // arrange
        var request = Request(qty: qty);

        // assert
        (Validation.PositiveQuantity(request) is null).Is(positiveAccepts);
        (Validation.PositiveOrZeroQuantity(request) is null).Is(positiveOrZeroAccepts);
    }

    /// <summary>
    /// A price of zero is refused where a price is required and required where the order type carries none —
    /// the pair a market order and a limit order need opposite answers from.
    /// </summary>
    /// <param name="price">The price to validate.</param>
    /// <param name="positiveAccepts">Whether the strictly-positive check should accept it.</param>
    /// <param name="zeroAccepts">Whether the must-be-zero check should accept it.</param>
    [Theory]
    [InlineData(1, true, false)]
    [InlineData(0, false, true)]
    [InlineData(-1, false, false)]
    public void PriceChecks_AreOppositesAboutZero(int price, bool positiveAccepts, bool zeroAccepts)
    {
        // arrange
        var request = Request(price: price);

        // assert
        (Validation.PositivePrice(request) is null).Is(positiveAccepts);
        (Validation.ZeroPrice(request) is null).Is(zeroAccepts);
    }

    /// <summary>
    /// The same opposition for the trigger price, which a stop or take-profit order requires and every other
    /// type must leave unset.
    /// </summary>
    /// <param name="levelPrice">The trigger price to validate.</param>
    /// <param name="positiveAccepts">Whether the strictly-positive check should accept it.</param>
    /// <param name="zeroAccepts">Whether the must-be-zero check should accept it.</param>
    [Theory]
    [InlineData(1, true, false)]
    [InlineData(0, false, true)]
    [InlineData(-1, false, false)]
    public void TriggerPriceChecks_AreOppositesAboutZero(int levelPrice, bool positiveAccepts, bool zeroAccepts)
    {
        // arrange
        var request = Request(levelPrice: levelPrice);

        // assert
        (Validation.PositiveTriggerPrice(request) is null).Is(positiveAccepts);
        (Validation.ZeroTriggerPrice(request) is null).Is(zeroAccepts);
    }

    /// <summary>
    /// A symbol that is missing, empty or only whitespace is refused; a real one is accepted.
    /// </summary>
    /// <param name="symbol">The symbol to validate.</param>
    /// <param name="accepts">Whether the check should accept it.</param>
    [Theory]
    [InlineData("BTCUSDT", true)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    public void Symbol_MustBeSet(string symbol, bool accepts)
    {
        // arrange
        var request = Request(symbol: symbol);

        // assert
        (Validation.Symbol(request) is null).Is(accepts);
    }

    /// <summary>
    /// A refusal carries the bad-request status and says which term was wrong, so a caller reading only the
    /// result can tell a rejected quantity from a rejected price.
    /// </summary>
    [Fact]
    public void Refusals_SayWhichTermWasWrong()
    {
        // assert
        var quantity = Validation.PositiveQuantity(Request(qty: 0)).NotNull();
        quantity.Status.Is(UserOperationStatus.BadRequest);
        quantity.Message.Is("invalid quantity");

        var price = Validation.PositivePrice(Request(price: 0)).NotNull();
        price.Message.Is("invalid price");

        var trigger = Validation.PositiveTriggerPrice(Request(levelPrice: 0)).NotNull();
        trigger.Message.Is("invalid trigger price");

        var symbol = Validation.Symbol(Request(symbol: "")).NotNull();
        symbol.Message.Is("empty symbol");
    }

    /// <summary>
    /// Builds a request with the given terms, defaulting every other one to something valid so each test
    /// varies only what it is about.
    /// </summary>
    /// <param name="symbol">The instrument symbol.</param>
    /// <param name="qty">The order quantity.</param>
    /// <param name="price">The limit price.</param>
    /// <param name="levelPrice">The trigger price.</param>
    /// <returns>The request.</returns>
    private static IInitOrderRequest Request(
        string symbol = "BTCUSDT",
        decimal qty = 1m,
        decimal price = 1m,
        decimal levelPrice = 1m
    ) => InitStopLossLimitOrder("id", OrientationRange.Both, symbol, OrderSide.Buy, qty, price, levelPrice);
}
