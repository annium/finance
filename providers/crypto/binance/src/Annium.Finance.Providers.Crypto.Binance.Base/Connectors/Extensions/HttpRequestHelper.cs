using System;
using System.Net.Http;
using System.Threading.Tasks;
using Annium.Finance.Providers.Crypto.Binance.Base.Contracts.Shared.Domain;
using Annium.Net.Http;

namespace Annium.Finance.Providers.Crypto.Binance.Base.Connectors.Extensions;

public static class HttpRequestHelper
{
    public static async Task<OperationResult> GetFailure(HttpFailureReason reason, HttpContent content, Exception? e) =>
        reason switch
        {
            HttpFailureReason.Abort => new OperationResult(1, "Request aborted"),
            HttpFailureReason.Parse
                => new OperationResult(1, $"Response parse failed. Content: {await content.ReadAsStringAsync()}"),
            HttpFailureReason.Exception
                => new OperationResult(
                    1,
                    $"Request failed. Error: {e?.Message}. Content: {await content.ReadAsStringAsync()}"
                ),
            _ => new OperationResult(1, "Unmapped failure")
        };
}
