using System;

namespace Annium.Finance.Providers.Shared.Loaders;

public interface IKeyedLoader<TKey, TContext, TData> : IDisposable
    where TKey : notnull
{
    event Action<TKey, TContext, TData> OnData;
    void Request(TKey key);
}
