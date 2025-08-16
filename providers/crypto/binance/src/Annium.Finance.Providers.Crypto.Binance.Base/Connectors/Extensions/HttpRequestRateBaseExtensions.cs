using System;
using System.Linq;
using System.Threading.Tasks;
using Annium.Logging;
using Annium.Net.Http;

namespace Annium.Finance.Providers.Crypto.Binance.Base.Connectors.Extensions;

public static class HttpRequestRateBaseExtensions
{
    private static readonly string[] _headerMasks = ["x-mbx-used-weight", "x-mbx-order"];

    public static IHttpRequest WithLogFromWithHeaders<T>(this IHttpRequest request, T subject, LogData log = default)
        where T : ILogSubject
    {
        return request.WithLogFrom(subject, log, _headerMasks);
    }

    public static IHttpRequest WithRateDelayBase(this IHttpRequest request, string interval, int watermark) =>
        request.Intercept(async next =>
        {
            var response = await next();

            var headerName = $"x-mbx-used-weight-{interval}";
            var usedHeader =
                response.Headers.FirstOrDefault(x => x.Key.ToLowerInvariant() == headerName).Value?.ToArray() ?? [];
            if (usedHeader.Length == 0)
            {
                request.Warn<string>("{headerName} header not present", headerName);
                await Task.Delay(TimeSpan.FromSeconds(2));
                return response;
            }

            if (!int.TryParse(usedHeader[0], out var used))
            {
                request.Error<string, string>(
                    "{headerName} header failed to parse from {usedHeader}",
                    headerName,
                    usedHeader[0]
                );
                await Task.Delay(TimeSpan.FromSeconds(2));
                return response;
            }

            if (used > watermark)
                await Task.Delay(TimeSpan.FromSeconds(2));

            return response;
        });
}
