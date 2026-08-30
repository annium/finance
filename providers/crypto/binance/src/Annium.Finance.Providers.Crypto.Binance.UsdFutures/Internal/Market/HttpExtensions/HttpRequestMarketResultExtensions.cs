using System.Net;
using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Domain.Market.Operations;
using Annium.Finance.Providers.Core.Market.HttpExtensions;
using Annium.Finance.Providers.Crypto.Binance.Base.Shared.Contracts.Domain;
using Annium.Net.Http;
using OneOf;
using static Annium.Finance.Providers.Crypto.Binance.Base.Shared.HttpExtensions.HttpRequestHelper;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Market.HttpExtensions;

/// <summary>
/// Sends a market data HTTP request and maps its response, including Binance's <see cref="OperationResult"/>
/// error envelope, into a <see cref="MarketResult{T}"/>.
/// </summary>
internal static class HttpRequestMarketResultExtensions
{
    /// <summary>
    /// Sends the request and maps its response into a market result, treating network/transport failures via
    /// <see cref="Annium.Finance.Providers.Crypto.Binance.Base.Shared.HttpExtensions.HttpRequestHelper.GetFailureAsync"/>.
    /// </summary>
    /// <typeparam name="T">The type of the successful response payload.</typeparam>
    /// <param name="request">The market data HTTP request.</param>
    /// <returns>A market result carrying the response payload, or a failure status if the request did not succeed.</returns>
    public static Task<MarketResult<T?>> AsMarketResultAsync<T>(this IHttpRequest request)
        where T : class
    {
        return request.AsMarketResultAsync<T, OperationResult>(GetFailureAsync, MapResponse);
    }

    /// <summary>
    /// Maps a completed HTTP response, which is either the expected payload or a Binance
    /// <see cref="OperationResult"/> error envelope, into a market result.
    /// </summary>
    /// <typeparam name="T">The type of the successful response payload.</typeparam>
    /// <param name="response">The HTTP response, holding either the payload or an error envelope.</param>
    /// <returns>A market result reflecting the payload on success, or the mapped error status on failure.</returns>
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

    /// <summary>
    /// Maps an HTTP status code to a market operation status, recognizing Binance's 418 "IP banned" status
    /// alongside the standard 429/400 codes.
    /// </summary>
    /// <param name="code">The HTTP status code of the response.</param>
    /// <returns>The corresponding market operation status.</returns>
    private static MarketOperationStatus MapStatusCode(HttpStatusCode code) =>
        code switch
        {
            (HttpStatusCode)418 or HttpStatusCode.TooManyRequests => MarketOperationStatus.TooManyRequests,
            HttpStatusCode.BadRequest => MarketOperationStatus.BadRequest,
            _ => MarketOperationStatus.UnknownError,
        };

    /// <summary>
    /// Maps a Binance <see cref="OperationResult"/> error code to a market operation status.
    /// </summary>
    /// <param name="code">The error code reported in the operation result.</param>
    /// <returns>The corresponding market operation status.</returns>
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
