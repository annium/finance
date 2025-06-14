using System;

namespace Annium.Finance.Providers.Shared.Loaders;

public interface ICompositeLoader<T> : IDisposable
{
    event Action<T> OnData;
    void Start(bool reportStatus);
    void Stop();
    void Request();
}
