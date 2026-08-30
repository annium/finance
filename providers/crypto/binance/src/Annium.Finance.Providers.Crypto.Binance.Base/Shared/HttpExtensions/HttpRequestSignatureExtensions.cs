using Annium.Finance.Providers.Crypto.Binance.Base.User.Services;
using Annium.Net.Http;

namespace Annium.Finance.Providers.Crypto.Binance.Base.Shared.HttpExtensions;

/// <summary>Extension methods for authenticating requests to Binance's SIGNED and USER_DATA endpoints.</summary>
public static class HttpRequestSignatureExtensions
{
    /// <summary>Fully authenticates a signed request: adds the API key header and the <c>timestamp</c>/<c>signature</c> query parameters.</summary>
    /// <param name="request">The request to sign.</param>
    /// <param name="ss">The signature service providing the API key, server time and signing.</param>
    /// <returns>The request, for chaining.</returns>
    public static IHttpRequest Sign(this IHttpRequest request, ISignatureService ss) => request.Key(ss).Signature(ss);

    /// <summary>Adds the <c>x-mbx-apikey</c> header identifying the account, as required by USER_STREAM and USER_DATA endpoints.</summary>
    /// <param name="request">The request to add the API key header to.</param>
    /// <param name="ss">The signature service providing the API key.</param>
    /// <returns>The request, for chaining.</returns>
    public static IHttpRequest Key(this IHttpRequest request, ISignatureService ss) =>
        request.Header("x-mbx-apikey", ss.GetKey());

    /// <summary>Adds a 30-second <c>recvWindow</c> query parameter, the window within which Binance accepts the request's timestamp.</summary>
    /// <param name="request">The request to add the receive window parameter to.</param>
    /// <returns>The request, for chaining.</returns>
    public static IHttpRequest ReceiveWindow(this IHttpRequest request) => request.Param("recvWindow", 30_000);

    /// <summary>Adds the <c>timestamp</c> query parameter and the HMAC-SHA256 <c>signature</c> computed over the resulting query string, as required by SIGNED endpoints.</summary>
    /// <param name="request">The request to sign.</param>
    /// <param name="ss">The signature service providing the server time and signing.</param>
    /// <returns>The request, for chaining.</returns>
    private static IHttpRequest Signature(this IHttpRequest request, ISignatureService ss) =>
        request.Configure(req =>
        {
            // set timestamp
            req.Param("timestamp", ss.ServerTime);

            // calculate signature
            req.NoParam("signature");
            var query = req.Uri.Query.TrimStart('?');
            var signature = ss.GetSignature(query);

            // add signature to query params
            req.Param("signature", signature);
        });
}
