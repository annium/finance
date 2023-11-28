using System;
using System.Net;
using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Domain.Operations;
using Annium.Net.Http;

namespace Annium.Finance.Providers.Shared.Connectors.Extensions;

public static class HttpRequestResultExtensions
{
    public static async Task<MarketResult<TData>> AsMarketResultAsync<TData, TError>(
        this IHttpRequest request,
        TData defaultValue,
        Func<TError, string> getError
    )
    {
        var response = await request.AsResponseAsync<TData, TError>(defaultValue);

        return response.Data.Match(
            MarketResult.Ok,
            error =>
            {
                var operationStatus = MapHttpStatusCodeToOperationStatus(response.StatusCode);
                var errorMessage = getError(error);

                return MarketResult.New(operationStatus, defaultValue, errorMessage);
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
