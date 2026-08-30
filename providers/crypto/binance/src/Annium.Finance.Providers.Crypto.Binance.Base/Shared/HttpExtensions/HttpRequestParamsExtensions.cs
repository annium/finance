using System.Collections.Generic;
using Annium.Net.Http;

namespace Annium.Finance.Providers.Crypto.Binance.Base.Shared.HttpExtensions;

/// <summary>Extension methods for adding a batch of query parameters to a Binance HTTP request.</summary>
public static class HttpRequestParamsExtensions
{
    /// <summary>Adds each entry of the given dictionary as a query parameter of the request.</summary>
    /// <param name="request">The request to add query parameters to.</param>
    /// <param name="queryParams">The query parameter names and values to add.</param>
    /// <returns>The request, for chaining.</returns>
    public static IHttpRequest Params(this IHttpRequest request, IReadOnlyDictionary<string, string> queryParams)
    {
        foreach (var (name, value) in queryParams)
            request.Param(name, value);

        return request;
    }
}
