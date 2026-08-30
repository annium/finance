using System;
using System.Threading.Tasks;
using Annium.Finance.Providers.Crypto.Binance.Base.Shared.Contracts.Domain;
using Annium.Net.Http;

namespace Annium.Finance.Providers.Crypto.Binance.Base.Shared.HttpExtensions;

/// <summary>Builds an <see cref="OperationResult"/> describing a transport-level (network, abort or parse) HTTP failure.</summary>
public static class HttpRequestHelper
{
    /// <summary>Builds an <see cref="OperationResult"/> for a request that failed before Binance returned a structured error body.</summary>
    /// <param name="reason">The reason the request failed.</param>
    /// <param name="response">The response received, if any, used for status/content details.</param>
    /// <param name="e">The exception that caused the failure, if any.</param>
    /// <returns>A task that resolves to the operation result describing the failure.</returns>
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
