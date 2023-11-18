using System;

namespace Annium.Finance.Providers.Shared.Services;

public interface ICompositeLoader<T> : IDisposable
{
    event Action<T> OnData;
    public void Start();
    public void Stop();
    public void Request();
}
