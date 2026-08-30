using System;
using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Domain.User.Operations;
using Annium.Net.Http;
using OneOf;

namespace Annium.Finance.Providers.Core.User.HttpExtensions;

/// <summary>
/// Extension methods for adapting <see cref="IHttpRequest"/> responses into <see cref="UserResult{T}"/>.
/// </summary>
public static class HttpRequestUserResultExtensions
{
    /// <summary>
    /// Sends the request and maps its response into a <see cref="UserResult{T}"/>.
    /// </summary>
    /// <typeparam name="TData">The type of successful response data.</typeparam>
    /// <typeparam name="TError">The type describing a business-level (non-transport) failure response.</typeparam>
    /// <param name="request">The request to send.</param>
    /// <param name="getFailure">The delegate that builds a <typeparamref name="TError"/> from a transport-level failure.</param>
    /// <param name="mapResponse">The delegate that maps the resulting response into a user result.</param>
    /// <returns>The mapped user result.</returns>
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
