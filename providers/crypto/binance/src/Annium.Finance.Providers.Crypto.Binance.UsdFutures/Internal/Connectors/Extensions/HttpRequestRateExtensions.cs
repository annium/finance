using Annium.Finance.Providers.Crypto.Binance.Base.Connectors.Extensions;
using Annium.Net.Http;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Connectors.Extensions;

internal static class HttpRequestRateExtensions
{
    private static int _waterMark = 2000;

    public static void UpdateRequestWeightLimit(int limit)
    {
        _waterMark = (limit * .8).FloorInt32();
    }

    public static IHttpRequest WithRateDelay1M(this IHttpRequest request) =>
        request.WithRateDelayBase("1m", _waterMark);
}
