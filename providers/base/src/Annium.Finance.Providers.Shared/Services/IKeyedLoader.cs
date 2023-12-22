using System;

namespace Annium.Finance.Providers.Shared.Services;

public interface IKeyedLoader<TKey, TContext, TData> : IDisposable
    where TKey : notnull
{
    event Action<TKey, TContext, TData> OnData;
    public void RequestUpdate(TKey key);
}
