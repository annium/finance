using Annium.Logging;
using Annium.Net.Http;

namespace Annium.Finance.Providers.Crypto.Binance.Base.Shared.HttpExtensions;

/// <summary>Extension methods for logging Binance HTTP requests while masking Binance-specific response headers.</summary>
public static class HttpRequestLogExtensions
{
    /// <summary>The Binance response header name prefixes to mask in logs, e.g. the used-weight and order-count headers.</summary>
    private static readonly string[] _headerMasks = ["x-mbx-used-weight", "x-mbx-order"];

    /// <summary>Attaches request/response logging to the request, masking Binance's <c>x-mbx-used-weight</c> and <c>x-mbx-order</c> headers.</summary>
    /// <typeparam name="T">The type of the logging subject.</typeparam>
    /// <param name="request">The request to attach logging to.</param>
    /// <param name="subject">The subject the log entries are attributed to.</param>
    /// <param name="log">The parts of the request/response to log.</param>
    /// <returns>The request, for chaining.</returns>
    public static IHttpRequest WithLogFromWithHeaders<T>(this IHttpRequest request, T subject, LogData log = default)
        where T : ILogSubject
    {
        return request.WithLogFrom(subject, log, _headerMasks);
    }
}
