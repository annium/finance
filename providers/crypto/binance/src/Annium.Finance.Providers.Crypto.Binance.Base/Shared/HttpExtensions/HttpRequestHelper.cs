using System;
using System.Threading.Tasks;
using Annium.Finance.Providers.Crypto.Binance.Base.Shared.Contracts.Domain;
using Annium.Net.Http;

namespace Annium.Finance.Providers.Crypto.Binance.Base.Shared.HttpExtensions;

public static class HttpRequestHelper
{
    public static async Task<OperationResult> GetFailureAsync(
        HttpFailureReason reason,
        IHttpResponse response,
        Exception? e
    )
    {
        var result = reason switch
        {
            HttpFailureReason.Network => new OperationResult(
                OperationResult.NetworkError,
                $"Request not sent ({response.StatusCode} - {response.StatusText})"
            ),
            HttpFailureReason.Abort => new OperationResult(
                OperationResult.Aborted,
                $"Request aborted ({response.StatusCode} - {response.StatusText})"
            ),
            HttpFailureReason.Parse => new OperationResult(
                OperationResult.ParseError,
                $"Response parse failed. Content: {await response.Content.ReadAsStringAsync()}"
            ),
            _ => new OperationResult(
                OperationResult.ParseError,
                $"Request failed. Error: {e?.Message}. Content: {await response.Content.ReadAsStringAsync()}"
            ),
        };

        return result;
    }
}
