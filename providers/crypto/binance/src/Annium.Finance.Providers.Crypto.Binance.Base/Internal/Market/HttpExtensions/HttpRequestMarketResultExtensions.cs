using System.Net;
using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Domain.Market.Operations;
using Annium.Finance.Providers.Core.Market.HttpExtensions;
using Annium.Finance.Providers.Crypto.Binance.Base.Shared.Contracts.Domain;
using Annium.Net.Http;
using OneOf;
using static Annium.Finance.Providers.Crypto.Binance.Base.Shared.HttpExtensions.HttpRequestHelper;

namespace Annium.Finance.Providers.Crypto.Binance.Base.Internal.Market.HttpExtensions;

/// <summary>
/// Adapts raw Binance market-data HTTP responses, that either deserialize to a payload or to an <see cref="OperationResult"/> error, into <see cref="MarketResult{T}"/>.
/// </summary>
internal static class HttpRequestMarketResultExtensions
{
    /// <summary>Sends the request and maps its response into a <see cref="MarketResult{T}"/>, converting an unsuccessful Binance error response into the matching status.</summary>
    /// <typeparam name="T">The type of the expected success payload.</typeparam>
    /// <param name="request">The market-data HTTP request to send.</param>
    /// <returns>A task that resolves to the mapped market result.</returns>
    public static Task<MarketResult<T?>> AsMarketResultAsync<T>(this IHttpRequest request)
        where T : class
    {
        return request.AsMarketResultAsync<T, OperationResult>(GetFailureAsync, MapResponse);
    }

    /// <summary>Maps a response that resolved to either a success payload or an <see cref="OperationResult"/> error into a <see cref="MarketResult{T}"/>.</summary>
    /// <typeparam name="T">The type of the expected success payload.</typeparam>
    /// <param name="response">The HTTP response carrying either the payload or the Binance error.</param>
    /// <returns>The mapped market result.</returns>
    private static MarketResult<T?> MapResponse<T>(IHttpResponse<OneOf<T, OperationResult>> response)
    {
        // if response mapped to success
        if (response.Data.IsT0)
        {
            var data = response.Data.AsT0;

            // if response is successful - return Ok
            if (response.IsSuccess)
                return MarketResult.Ok<T?>(data);

            // otherwise response is mapped to success, but is failure - use response.StatusCode
            {
                var status = MapStatusCode(response.StatusCode);
                return MarketResult.New<T?>(status, data);
            }
        }

        // if response mapped to error, OperationResult - use it to construct response
        {
            var error = response.Data.AsT1;
            var status = MapOperationCode(error.Code);

            return MarketResult.New<T?>(status, default, error.Message);
        }
    }

    /// <summary>Maps an HTTP status code from an otherwise-successfully-parsed Binance response into a market operation status.</summary>
    /// <param name="code">The HTTP status code returned by Binance.</param>
    /// <returns>The equivalent market operation status.</returns>
    private static MarketOperationStatus MapStatusCode(HttpStatusCode code) =>
        code switch
        {
            (HttpStatusCode)418 or HttpStatusCode.TooManyRequests => MarketOperationStatus.TooManyRequests,
            HttpStatusCode.BadRequest => MarketOperationStatus.BadRequest,
            _ => MarketOperationStatus.UnknownError,
        };

    /// <summary>Maps a Binance <see cref="OperationResult"/> error code into a market operation status.</summary>
    /// <param name="code">The error code returned by Binance in the operation result.</param>
    /// <returns>The equivalent market operation status.</returns>
    private static MarketOperationStatus MapOperationCode(long code) =>
        code switch
        {
            OperationResult.NetworkError => MarketOperationStatus.NetworkError,
            OperationResult.Aborted => MarketOperationStatus.Aborted,
            OperationResult.ParseError => MarketOperationStatus.ParseError,
            < 0 => MarketOperationStatus.BadRequest,
            _ => MarketOperationStatus.UnknownError,
        };
}
