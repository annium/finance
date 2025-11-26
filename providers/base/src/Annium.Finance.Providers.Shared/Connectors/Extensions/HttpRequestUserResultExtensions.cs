using System;
using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Domain.Operations;
using Annium.Net.Http;
using OneOf;

namespace Annium.Finance.Providers.Shared.Connectors.Extensions;

public static class HttpRequestUserResultExtensions
{
    public static async Task<UserResult<TData?>> AsUserResultAsync<TData, TError>(
        this IHttpRequest request,
        Func<HttpFailureReason, IHttpResponse, Exception?, Task<TError>> getFailure,
        Func<IHttpResponse<OneOf<TData, TError>>, UserResult<TData?>> mapResponse
    )
        where TData : class
    {
        var response = await request.AsResponseAsync<TData, TError>(getFailure);
        var result = mapResponse(response);

        return result;
    }
}
