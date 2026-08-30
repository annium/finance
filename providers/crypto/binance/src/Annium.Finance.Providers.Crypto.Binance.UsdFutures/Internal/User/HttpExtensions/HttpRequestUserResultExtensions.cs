using System.Net;
using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Domain.User.Operations;
using Annium.Finance.Providers.Core.User.HttpExtensions;
using Annium.Finance.Providers.Crypto.Binance.Base.Shared.Contracts.Domain;
using Annium.Net.Http;
using OneOf;
using static Annium.Finance.Providers.Crypto.Binance.Base.Shared.HttpExtensions.HttpRequestHelper;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.User.HttpExtensions;

/// <summary>
/// Sends a user data HTTP request and maps its response, including Binance's <see cref="OperationResult"/> error
/// envelope, into a <see cref="UserResult{T}"/>.
/// </summary>
internal static class HttpRequestUserResultExtensions
{
    /// <summary>
    /// Sends the request and maps its response into a user result, treating network/transport failures via
    /// <see cref="Annium.Finance.Providers.Crypto.Binance.Base.Shared.HttpExtensions.HttpRequestHelper.GetFailureAsync"/>.
    /// </summary>
    /// <typeparam name="T">The type of the successful response payload.</typeparam>
    /// <param name="request">The user data HTTP request.</param>
    /// <returns>A user result carrying the response payload, or a failure status if the request did not succeed.</returns>
    public static Task<UserResult<T?>> AsUserResultAsync<T>(this IHttpRequest request)
        where T : class
    {
        return request.AsUserResultAsync<T, OperationResult>(GetFailureAsync, MapResponse);
    }

    /// <summary>
    /// Maps a completed HTTP response, which is either the expected payload or a Binance
    /// <see cref="OperationResult"/> error envelope, into a user result.
    /// </summary>
    /// <typeparam name="T">The type of the successful response payload.</typeparam>
    /// <param name="response">The HTTP response, holding either the payload or an error envelope.</param>
    /// <returns>A user result reflecting the payload on success, or the mapped error status on failure.</returns>
    private static UserResult<T?> MapResponse<T>(IHttpResponse<OneOf<T, OperationResult>> response)
    {
        // if response mapped to success
        if (response.Data.IsT0)
        {
            var data = response.Data.AsT0;

            // if response is successful - return Ok
            if (response.IsSuccess)
                return UserResult.Ok<T?>(data);

            // otherwise response is mapped to success, but is failure - use response.StatusCode
            {
                var status = MapStatusCode(response.StatusCode);
                return UserResult.New<T?>(status, data);
            }
        }

        // if response mapped to error, OperationResult - use it to construct response
        {
            var error = response.Data.AsT1;
            var status = MapOperationCode(error.Code);

            return UserResult.New<T?>(status, default, error.Message);
        }
    }

    /// <summary>
    /// Maps an HTTP status code to a user operation status, recognizing Binance's 418 "IP banned" status
    /// alongside the standard 429/400/401/403/404 codes.
    /// </summary>
    /// <param name="code">The HTTP status code of the response.</param>
    /// <returns>The corresponding user operation status.</returns>
    private static UserOperationStatus MapStatusCode(HttpStatusCode code) =>
        code switch
        {
            (HttpStatusCode)418 or HttpStatusCode.TooManyRequests => UserOperationStatus.TooManyRequests,
            HttpStatusCode.BadRequest => UserOperationStatus.BadRequest,
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => UserOperationStatus.Forbidden,
            HttpStatusCode.NotFound => UserOperationStatus.NotFound,
            _ => UserOperationStatus.UnknownError,
        };

    /// <summary>
    /// Maps a Binance <see cref="OperationResult"/> error code to a user operation status, recognizing the
    /// balance/margin insufficiency codes specifically alongside the generic negative-code convention.
    /// </summary>
    /// <param name="code">The error code reported in the operation result.</param>
    /// <returns>The corresponding user operation status.</returns>
    private static UserOperationStatus MapOperationCode(long code) =>
        code switch
        {
            OperationResult.NetworkError => UserOperationStatus.NetworkError,
            OperationResult.Aborted => UserOperationStatus.Aborted,
            OperationResult.ParseError => UserOperationStatus.ParseError,
            -2018 => UserOperationStatus.InsufficientBalance, // BALANCE_NOT_SUFFICIENT
            -2019 => UserOperationStatus.InsufficientBalance, // MARGIN_NOT_SUFFICIENT
            < 0 => UserOperationStatus.BadRequest,
            _ => UserOperationStatus.UnknownError,
        };
}
