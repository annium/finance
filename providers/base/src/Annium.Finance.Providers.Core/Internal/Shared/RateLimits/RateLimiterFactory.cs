using Annium.Finance.Providers.Core.Shared.RateLimits;
using Annium.Logging;

namespace Annium.Finance.Providers.Core.Internal.Shared.RateLimits;

internal class RateLimiterFactory(ILogger logger) : IRateLimiterFactory
{
    public IRateLimiter CreateRateLimiter(int limit, int lowerWeightValue, int lowerWeightDelay)
    {
        return new RateLimiter(limit, lowerWeightValue, lowerWeightDelay, logger);
    }
}
