namespace Annium.Finance.Providers.Core.Shared.RateLimits;

public interface IRateLimiterFactory
{
    IRateLimiter CreateRateLimiter(int limit, int lowerWeightValue, int lowerWeightDelay);
}
