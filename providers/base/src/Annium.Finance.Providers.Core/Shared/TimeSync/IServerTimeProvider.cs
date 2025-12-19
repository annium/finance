using System;

namespace Annium.Finance.Providers.Core.Shared.TimeSync;

public interface IServerTimeProvider : IServerTimeSource
{
    event Action<bool> OnStateChanged;
}
