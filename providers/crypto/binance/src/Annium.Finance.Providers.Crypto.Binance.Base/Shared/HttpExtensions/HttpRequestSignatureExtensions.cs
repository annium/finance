using Annium.Finance.Providers.Crypto.Binance.Base.User.Services;
using Annium.Net.Http;

namespace Annium.Finance.Providers.Crypto.Binance.Base.Shared.HttpExtensions;

public static class HttpRequestSignatureExtensions
{
    public static IHttpRequest Sign(this IHttpRequest request, SignatureService ss) => request.Key(ss).Signature(ss);

    public static IHttpRequest Key(this IHttpRequest request, SignatureService ss) =>
        request.Header("x-mbx-apikey", ss.GetKey());

    public static IHttpRequest ReceiveWindow(this IHttpRequest request) => request.Param("recvWindow", 30_000);

    private static IHttpRequest Signature(this IHttpRequest request, SignatureService ss) =>
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
