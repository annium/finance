using System;
using System.Net;
using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Domain.Operations;
using Annium.Net.Http;

namespace Annium.Finance.Providers.Shared.Connectors.Extensions;

public static class HttpRequestResultExtensions
{
    public static async Task<MarketResult<TData?>> AsMarketResultAsync<TData, TError>(
        this IHttpRequest request,
        Func<HttpFailureReason, IHttpResponse, Exception?, Task<TError>> getFailure,
        Func<TError, MarketOperationStatus?> getErrorStatus,
        Func<TError, string> getError
    )
        where TData : class
    {
        var response = await request.AsResponseAsync<TData, TError>(getFailure);

        return response.Data.Match<MarketResult<TData?>>(
            MarketResult.Ok!,
            error =>
            {
                var operationStatus = getErrorStatus(error) ?? MapHttpStatusCodeToOperationStatus(response.StatusCode);
                var errorMessage = getError(error);

                return MarketResult.New<TData?>(operationStatus, null, errorMessage);
            }
        );
    }

    private static MarketOperationStatus MapHttpStatusCodeToOperationStatus(HttpStatusCode code) =>
        code switch
        {
            HttpStatusCode.BadRequest => MarketOperationStatus.BadRequest,
            _ => MarketOperationStatus.UnknownError
        };
}
