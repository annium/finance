using System;

namespace Annium.Finance.Providers.Shared.Loaders;

public interface ICompositeLoader<T> : IDisposable
{
    event Action<T> OnData;
    public void Start();
    public void Stop();
    public void Request();
}
