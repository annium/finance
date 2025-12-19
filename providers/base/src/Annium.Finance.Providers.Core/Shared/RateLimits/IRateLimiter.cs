using System;

namespace Annium.Finance.Providers.Core.Shared.RateLimits;

public interface IRateLimiter : IDisposable
{
    bool CanExecute();
    void UpdateLimit(int limit);
    void UsedWeight(int weight);
}
