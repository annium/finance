using System;

namespace Annium.Finance.Providers.Core.Shared.Loaders;

public interface IKeyedLoader<TKey, TContext, TData> : IDisposable
    where TKey : notnull
{
    event Action<TKey, TContext, TData> OnData;
    void Request(TKey key);
}
