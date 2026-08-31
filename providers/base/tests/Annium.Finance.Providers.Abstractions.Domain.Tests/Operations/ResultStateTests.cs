using Annium.Finance.Providers.Abstractions.Domain.Market.Operations;
using Annium.Finance.Providers.Abstractions.Domain.User.Operations;
using Annium.Testing;
using Xunit;

namespace Annium.Finance.Providers.Abstractions.Domain.Tests.Operations;

/// <summary>
/// Pins the four states a provider result sorts every outcome into. Transport failures, client-side aborts
/// and business failures are three different things — a network error is worth retrying, an abort was asked
/// for, a rejected order is not going to succeed on a second attempt — and these flags are how a caller tells
/// them apart without reading the status enum itself. They are derived in the constructors, so an error here
/// mislabels every result the type ever carries.
/// </summary>
public class ResultStateTests
{
    /// <summary>
    /// Verifies that a market result routes each status to exactly one of the four states, and that the three
    /// specific ones are mutually exclusive with the general failure flag.
    /// </summary>
    /// <param name="status">The status the result carries.</param>
    /// <param name="isSuccess">Whether the operation succeeded.</param>
    /// <param name="isNetworkError">Whether the request never reached the exchange.</param>
    /// <param name="isAborted">Whether the caller abandoned the request.</param>
    /// <param name="isFailure">Whether the exchange answered, and refused.</param>
    [Theory]
    [InlineData(MarketOperationStatus.Ok, true, false, false, false)]
    [InlineData(MarketOperationStatus.NetworkError, false, true, false, false)]
    [InlineData(MarketOperationStatus.Aborted, false, false, true, false)]
    [InlineData(MarketOperationStatus.NotConnected, false, false, false, true)]
    [InlineData(MarketOperationStatus.TooManyRequests, false, false, false, true)]
    [InlineData(MarketOperationStatus.BadRequest, false, false, false, true)]
    [InlineData(MarketOperationStatus.NotFound, false, false, false, true)]
    [InlineData(MarketOperationStatus.ParseError, false, false, false, true)]
    [InlineData(MarketOperationStatus.UnknownError, false, false, false, true)]
    public void MarketResult_SortsEveryStatusIntoOneState(
        MarketOperationStatus status,
        bool isSuccess,
        bool isNetworkError,
        bool isAborted,
        bool isFailure
    )
    {
        // assert - the plain result and the one carrying data derive these independently, so check both
        var plain = MarketResult.New(status, "message");
        plain.IsSuccess.Is(isSuccess);
        plain.IsNetworkError.Is(isNetworkError);
        plain.IsAborted.Is(isAborted);
        plain.IsFailure.Is(isFailure);

        var withData = MarketResult.New<string?>(status, null, "message");
        withData.IsSuccess.Is(isSuccess);
        withData.IsNetworkError.Is(isNetworkError);
        withData.IsAborted.Is(isAborted);
        withData.IsFailure.Is(isFailure);
    }

    /// <summary>
    /// Verifies that carrying a result forward keeps its outcome. Each of the three <c>From</c> overloads
    /// exists to move a status and message onto a differently-shaped result — dropping data, attaching data,
    /// or replacing it — and a failure that loses its status on the way becomes a success carrying whatever
    /// empty payload the caller supplied. That is the shape a rejected order takes when its validation result
    /// is handed on: an <c>Ok</c> result with an empty parameter set, submitted as if it had passed.
    /// </summary>
    [Fact]
    public void MarketResult_CarriedForward_KeepsItsOutcome()
    {
        // arrange
        var failed = MarketResult.New(MarketOperationStatus.BadRequest, "rejected");
        var failedWithData = MarketResult.New<string?>(MarketOperationStatus.BadRequest, null, "rejected");

        // assert - data dropped
        var dropped = MarketResult.From(failedWithData);
        dropped.Status.Is(MarketOperationStatus.BadRequest);
        dropped.Message.Is("rejected");
        dropped.IsSuccess.IsFalse();

        // assert - data attached to a data-less result
        var attached = MarketResult.From(failed, "payload");
        attached.Status.Is(MarketOperationStatus.BadRequest, "attaching data must not turn a refusal into a success");
        attached.Message.Is("rejected");
        attached.Data.Is("payload");

        // assert - data replaced
        var replaced = MarketResult.From(failedWithData, 42);
        replaced.Status.Is(MarketOperationStatus.BadRequest);
        replaced.Message.Is("rejected");
        replaced.Data.Is(42);
    }

    /// <summary>
    /// Verifies the same on the user side. This is the live path: a rejected order request's validation result
    /// is carried onto the query the connector would send, and a status lost here submits a malformed order.
    /// </summary>
    [Fact]
    public void UserResult_CarriedForward_KeepsItsOutcome()
    {
        // arrange
        var failed = UserResult.New(UserOperationStatus.BadRequest, "rejected");
        var failedWithData = UserResult.New<string?>(UserOperationStatus.BadRequest, null, "rejected");

        // assert - data dropped
        var dropped = UserResult.From(failedWithData);
        dropped.Status.Is(UserOperationStatus.BadRequest);
        dropped.Message.Is("rejected");
        dropped.IsSuccess.IsFalse();

        // assert - data attached to a data-less result
        var attached = UserResult.From(failed, "payload");
        attached.Status.Is(UserOperationStatus.BadRequest, "attaching data must not turn a refusal into a success");
        attached.Message.Is("rejected");
        attached.Data.Is("payload");

        // assert - data replaced
        var replaced = UserResult.From(failedWithData, 42);
        replaced.Status.Is(UserOperationStatus.BadRequest);
        replaced.Message.Is("rejected");
        replaced.Data.Is(42);
    }

    /// <summary>
    /// Verifies the same sorting on the user side, whose statuses cover the account operations rather than the
    /// market ones.
    /// </summary>
    /// <param name="status">The status the result carries.</param>
    /// <param name="isSuccess">Whether the operation succeeded.</param>
    /// <param name="isNetworkError">Whether the request never reached the exchange.</param>
    /// <param name="isAborted">Whether the caller abandoned the request.</param>
    /// <param name="isFailure">Whether the exchange answered, and refused.</param>
    [Theory]
    [InlineData(UserOperationStatus.Ok, true, false, false, false)]
    [InlineData(UserOperationStatus.NetworkError, false, true, false, false)]
    [InlineData(UserOperationStatus.Aborted, false, false, true, false)]
    [InlineData(UserOperationStatus.BadRequest, false, false, false, true)]
    [InlineData(UserOperationStatus.InsufficientBalance, false, false, false, true)]
    [InlineData(UserOperationStatus.TooManyRequests, false, false, false, true)]
    [InlineData(UserOperationStatus.ParseError, false, false, false, true)]
    [InlineData(UserOperationStatus.UnknownError, false, false, false, true)]
    public void UserResult_SortsEveryStatusIntoOneState(
        UserOperationStatus status,
        bool isSuccess,
        bool isNetworkError,
        bool isAborted,
        bool isFailure
    )
    {
        // assert
        var plain = UserResult.New(status, "message");
        plain.IsSuccess.Is(isSuccess);
        plain.IsNetworkError.Is(isNetworkError);
        plain.IsAborted.Is(isAborted);
        plain.IsFailure.Is(isFailure);

        var withData = UserResult.New<string?>(status, null, "message");
        withData.IsSuccess.Is(isSuccess);
        withData.IsNetworkError.Is(isNetworkError);
        withData.IsAborted.Is(isAborted);
        withData.IsFailure.Is(isFailure);
    }
}
