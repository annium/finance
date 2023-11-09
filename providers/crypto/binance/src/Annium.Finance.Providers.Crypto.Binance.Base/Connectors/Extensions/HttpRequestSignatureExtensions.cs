using Annium.Finance.Providers.Crypto.Binance.Base.Services;
using Annium.Net.Http;
using NodaTime;

namespace Annium.Finance.Providers.Crypto.Binance.Base.Connectors.Extensions;

public static class HttpRequestSignatureExtensions
{
    public static IHttpRequest Sign(this IHttpRequest request, SignatureService ss) =>
        request.Timestamp().Key(ss.GetKey()).Signature(ss);

    public static IHttpRequest Key(this IHttpRequest request, string key) => request.Header("x-mbx-apikey", key);

    public static IHttpRequest Timestamp(this IHttpRequest request, long timestamp) =>
        request.Param("timestamp", timestamp);

    public static IHttpRequest Timestamp(this IHttpRequest request) =>
        // TODO: use timestamp from signature service
        request.Timestamp(SystemClock.Instance.GetCurrentInstant().ToUnixTimeMilliseconds());

    public static IHttpRequest ReceiveWindow(this IHttpRequest request) => request.Param("recvWindow", 30_000);

    public static IHttpRequest Signature(this IHttpRequest request, SignatureService ss) =>
        request.Configure(req =>
        {
            var query = req.Uri.Query.TrimStart('?');
            var body = req.Content?.ReadAsStringAsync().Result ?? string.Empty;
            var signature = ss.GetSignature($"{query}{body}");

            req.Param("signature", signature);
        });
}
