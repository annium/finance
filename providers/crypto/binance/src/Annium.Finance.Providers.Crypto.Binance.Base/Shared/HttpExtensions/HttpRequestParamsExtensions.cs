using System.Collections.Generic;
using Annium.Net.Http;

namespace Annium.Finance.Providers.Crypto.Binance.Base.Shared.HttpExtensions;

public static class HttpRequestParamsExtensions
{
    public static IHttpRequest Params(this IHttpRequest request, IReadOnlyDictionary<string, string> queryParams)
    {
        foreach (var (name, value) in queryParams)
            request.Param(name, value);

        return request;
    }
}
