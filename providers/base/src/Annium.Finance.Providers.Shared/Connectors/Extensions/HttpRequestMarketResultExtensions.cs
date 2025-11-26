using System;
using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Domain.Operations;
using Annium.Net.Http;
using OneOf;

namespace Annium.Finance.Providers.Shared.Connectors.Extensions;

public static class HttpRequestMarketResultExtensions
{
    public static async Task<MarketResult<TData?>> AsMarketResultAsync<TData, TError>(
        this IHttpRequest request,
        Func<HttpFailureReason, IHttpResponse, Exception?, Task<TError>> getFailure,
        Func<IHttpResponse<OneOf<TData, TError>>, MarketResult<TData?>> mapResponse
    )
        where TData : class
    {
        var response = await request.AsResponseAsync<TData, TError>(getFailure);
        var result = mapResponse(response);

        return result;
    }
}
