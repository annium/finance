using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Annium.Core.DependencyInjection;
using Annium.Extensions.Pooling;
using Annium.Finance.Providers.Abstractions.Connectors.Connectors;
using Annium.Finance.Providers.Abstractions.Domain.Extensions;
using Annium.Finance.Providers.Abstractions.Domain.Interfaces;
using Annium.Finance.Providers.Abstractions.Domain.Models;
using Annium.Logging;
using Microsoft.Extensions.DependencyInjection;
using OneOf;

namespace Annium.Finance.Providers.Shared.Internal.Connectors;

internal abstract class ConnectorCacheProvider<TSettings, TConnector>
    : ObjectCacheProvider<TSettings, TConnector>,
        ILogSubject
    where TSettings : class, IConnectorSettings
    where TConnector : IConnectorBase
{
    public ILogger Logger { get; }
    private readonly IServiceProvider _sp;
    private readonly ConcurrentDictionary<TSettings, Entry> _scopes = new();

    protected ConnectorCacheProvider(IServiceProvider sp, ILogger logger)
    {
        Logger = logger;
        _sp = sp;
    }

    public override async Task<OneOf<TConnector, IDisposableReference<TConnector>>> CreateAsync(
        TSettings key,
        CancellationToken ct
    )
    {
        var providerKey = key.GetProviderKey();

        this.Trace("{key} - resolve entry for {settings}", providerKey, key);
        var entry = _scopes.GetOrAdd(key, CreateEntry);

        this.Trace("{key} - init {key} connector for {settings}", providerKey, key);
        await entry.Connector.InitAsync(); // this must not be called twice by design

        return entry.Connector;
    }

    public override async Task DisposeAsync(TSettings key, TConnector value)
    {
        var providerKey = key.GetProviderKey();

        this.Trace("resolve {key} entry for {settings}", providerKey, key);
        if (!_scopes.TryGetValue(key, out var entry))
        {
            this.Warn("resolved no {key} entry for {settings}", providerKey, key);
            return;
        }

        this.Warn("dispose {key} entry for {settings}", providerKey, key);
        await entry.DisposeAsync();

        this.Warn("resolved {key} entry for {settings}", providerKey, key);
    }

    protected abstract void Inject(IServiceProvider scopeProvider, TSettings settings);

    private Entry CreateEntry(TSettings settings)
    {
        var providerKey = ProviderKey.Create(settings.Provider, settings.Environment);

        this.Trace("create new {key} scope for {settings}", providerKey, settings);
        var scope = _sp.CreateAsyncScope();

        this.Trace("{key} - provide {settings} into scope", providerKey, settings);
        Inject(scope.ServiceProvider, settings);

        this.Trace("create new {key} connector for {settings}", providerKey, settings);
        var factory = _sp.ResolveKeyed<Func<IServiceProvider, TConnector>>(providerKey);
        var connector = factory(scope.ServiceProvider);

        return new Entry(scope, connector);
    }

    private record Entry(IAsyncDisposable Scope, TConnector Connector) : IAsyncDisposable
    {
        public ValueTask DisposeAsync()
        {
            return Scope.DisposeAsync();
        }
    }
}
