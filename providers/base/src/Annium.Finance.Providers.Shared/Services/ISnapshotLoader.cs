using System;

namespace Annium.Finance.Providers.Shared.Services;

public interface ISnapshotLoader<T> : IDisposable
{
    event Action<T> OnData;
    void Start(bool reportStatus);
    void Stop();
}
