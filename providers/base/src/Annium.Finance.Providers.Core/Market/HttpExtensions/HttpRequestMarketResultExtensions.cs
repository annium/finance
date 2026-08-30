using System;
using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Domain.Market.Operations;
using Annium.Net.Http;
using OneOf;

namespace Annium.Finance.Providers.Core.Market.HttpExtensions;

/// <summary>
/// Extension methods for adapting <see cref="IHttpRequest"/> responses into <see cref="MarketResult{T}"/>.
/// </summary>
public static class HttpRequestMarketResultExtensions
{
    /// <summary>
    /// Sends the request and maps its response into a <see cref="MarketResult{T}"/>.
    /// </summary>
    /// <typeparam name="TData">The type of successful response data.</typeparam>
    /// <typeparam name="TError">The type describing a business-level (non-transport) failure response.</typeparam>
    /// <param name="request">The request to send.</param>
    /// <param name="getFailure">The delegate that builds a <typeparamref name="TError"/> from a transport-level failure.</param>
    /// <param name="mapResponse">The delegate that maps the resulting response into a market result.</param>
    /// <returns>The mapped market result.</returns>
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
