using System;

namespace Annium.Finance.Providers.Shared.ServerTime;

public interface IServerTimeProvider : IServerTimeSource
{
    event Action<bool> OnStateChanged;
}
