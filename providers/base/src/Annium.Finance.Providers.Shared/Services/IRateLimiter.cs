using System;

namespace Annium.Finance.Providers.Shared.Services;

public interface IRateLimiter : IDisposable
{
    bool CanExecute();
    void UpdateLimit(int limit);
    void UsedWeight(int weight);
}
