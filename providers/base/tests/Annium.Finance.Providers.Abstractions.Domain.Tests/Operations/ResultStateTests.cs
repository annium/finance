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
