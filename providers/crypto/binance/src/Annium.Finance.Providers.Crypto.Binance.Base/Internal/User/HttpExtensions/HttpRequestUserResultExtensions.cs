using System.Net;
using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Domain.User.Operations;
using Annium.Finance.Providers.Core.User.HttpExtensions;
using Annium.Finance.Providers.Crypto.Binance.Base.Shared.Contracts.Domain;
using Annium.Net.Http;
using OneOf;
using static Annium.Finance.Providers.Crypto.Binance.Base.Shared.HttpExtensions.HttpRequestHelper;

namespace Annium.Finance.Providers.Crypto.Binance.Base.Internal.User.HttpExtensions;

/// <summary>
/// Adapts raw Binance account/trading HTTP responses, that either deserialize to a payload or to an <see cref="OperationResult"/> error, into <see cref="UserResult{T}"/>.
/// </summary>
internal static class HttpRequestUserResultExtensions
{
    /// <summary>Sends the signed request and maps its response into a <see cref="UserResult{T}"/>, converting an unsuccessful Binance error response into the matching status.</summary>
    /// <typeparam name="T">The type of the expected success payload.</typeparam>
    /// <param name="request">The account/trading HTTP request to send.</param>
    /// <returns>A task that resolves to the mapped user result.</returns>
    public static Task<UserResult<T?>> AsUserResultAsync<T>(this IHttpRequest request)
        where T : class
    {
        return request.AsUserResultAsync<T, OperationResult>(GetFailureAsync, MapResponse);
    }

    /// <summary>Maps a response that resolved to either a success payload or an <see cref="OperationResult"/> error into a <see cref="UserResult{T}"/>.</summary>
    /// <typeparam name="T">The type of the expected success payload.</typeparam>
    /// <param name="response">The HTTP response carrying either the payload or the Binance error.</param>
    /// <returns>The mapped user result.</returns>
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

    /// <summary>Maps an HTTP status code from an otherwise-successfully-parsed Binance response into a user operation status.</summary>
    /// <param name="code">The HTTP status code returned by Binance.</param>
    /// <returns>The equivalent user operation status.</returns>
    private static UserOperationStatus MapStatusCode(HttpStatusCode code) =>
        code switch
        {
            (HttpStatusCode)418 or HttpStatusCode.TooManyRequests => UserOperationStatus.TooManyRequests,
            HttpStatusCode.BadRequest => UserOperationStatus.BadRequest,
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => UserOperationStatus.Forbidden,
            HttpStatusCode.NotFound => UserOperationStatus.NotFound,
            _ => UserOperationStatus.UnknownError,
        };

    /// <summary>Maps a Binance <see cref="OperationResult"/> error code into a user operation status.</summary>
    /// <param name="code">The error code returned by Binance in the operation result.</param>
    /// <returns>The equivalent user operation status.</returns>
    private static UserOperationStatus MapOperationCode(long code) =>
        code switch
        {
            OperationResult.NetworkError => UserOperationStatus.NetworkError,
            OperationResult.Aborted => UserOperationStatus.Aborted,
            OperationResult.ParseError => UserOperationStatus.ParseError,
            < 0 => UserOperationStatus.BadRequest,
            _ => UserOperationStatus.UnknownError,
        };
}
