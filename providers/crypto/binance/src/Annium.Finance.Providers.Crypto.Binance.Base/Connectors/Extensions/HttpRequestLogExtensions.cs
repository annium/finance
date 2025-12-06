using Annium.Logging;
using Annium.Net.Http;

namespace Annium.Finance.Providers.Crypto.Binance.Base.Connectors.Extensions;

public static class HttpRequestLogExtensions
{
    private static readonly string[] _headerMasks = ["x-mbx-used-weight", "x-mbx-order"];

    public static IHttpRequest WithLogFromWithHeaders<T>(this IHttpRequest request, T subject, LogData log = default)
        where T : ILogSubject
    {
        return request.WithLogFrom(subject, log, _headerMasks);
    }
}
