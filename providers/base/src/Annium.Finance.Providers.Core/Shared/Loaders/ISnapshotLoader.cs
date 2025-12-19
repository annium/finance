using System;

namespace Annium.Finance.Providers.Core.Shared.Loaders;

public interface ISnapshotLoader<T> : IDisposable
{
    event Action<T> OnData;
    void Start(bool reportStatus);
    void Stop();
}
