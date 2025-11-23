using Annium.Finance.Providers.Shared.Services;
using Annium.Logging;

namespace Annium.Finance.Providers.Shared.Internal.Services;

internal class RateLimiterFactory(ILogger logger) : IRateLimiterFactory
{
    public IRateLimiter CreateRateLimiter(int limit, int lowerWeightValue, int lowerWeightDelay)
    {
        return new RateLimiter(limit, lowerWeightValue, lowerWeightDelay, logger);
    }
}
