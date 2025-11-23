namespace Annium.Finance.Providers.Shared.Services;

public interface IRateLimiterFactory
{
    IRateLimiter CreateRateLimiter(int limit, int lowerWeightValue, int lowerWeightDelay);
}
