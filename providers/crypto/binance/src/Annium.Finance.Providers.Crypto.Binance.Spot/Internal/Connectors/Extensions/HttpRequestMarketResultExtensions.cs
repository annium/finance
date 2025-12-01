using System.Net;
using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Domain.Operations;
using Annium.Finance.Providers.Crypto.Binance.Base.Contracts.Shared.Domain;
using Annium.Finance.Providers.Shared.Connectors.Extensions;
using Annium.Net.Http;
using OneOf;
using static Annium.Finance.Providers.Crypto.Binance.Base.Connectors.Extensions.HttpRequestHelper;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Internal.Connectors.Extensions;

internal static class HttpRequestMarketResultExtensions
{
    public static Task<MarketResult<T?>> AsMarketResultAsync<T>(this IHttpRequest request)
        where T : class
    {
        return request.AsMarketResultAsync<T, OperationResult>(GetFailureAsync, MapResponse);
    }

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

    private static MarketOperationStatus MapStatusCode(HttpStatusCode code) =>
        code switch
        {
            (HttpStatusCode)418 or HttpStatusCode.TooManyRequests => MarketOperationStatus.TooManyRequests,
            HttpStatusCode.BadRequest => MarketOperationStatus.BadRequest,
            _ => MarketOperationStatus.UnknownError,
        };

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
