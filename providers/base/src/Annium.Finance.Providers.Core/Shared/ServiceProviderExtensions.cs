using System;
using Annium.Core.DependencyInjection;
using Annium.Finance.Providers.Core.Internal.Shared.RateLimits;
using Annium.Finance.Providers.Core.Shared.RateLimits;
using Annium.Logging;

namespace Annium.Finance.Providers.Core.Shared;

public static class ServiceProviderExtensions
{
    public static IRateLimiter CreateRateLimiter(
        this IServiceProvider sp,
        int limit,
        int lowerWeightValue,
        int lowerWeightDelay
    )
    {
        var logger = sp.Resolve<ILogger>();

        return new RateLimiter(limit, lowerWeightValue, lowerWeightDelay, logger);
    }
}
