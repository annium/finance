using System;

namespace Annium.Finance.Providers.Core.Shared.ServerTime;

public interface IServerTimeProvider : IServerTimeSource
{
    event Action<bool> OnStateChanged;
}
