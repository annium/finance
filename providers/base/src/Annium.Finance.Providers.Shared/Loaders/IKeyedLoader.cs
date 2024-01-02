using System;

namespace Annium.Finance.Providers.Shared.Loaders;

public interface IKeyedLoader<TKey, TContext, TData> : IDisposable
    where TKey : notnull
{
    event Action<TKey, TContext, TData> OnData;
    public void Request(TKey key);
}
