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

internal class ConnectorCacheProvider<TConfig, TConnector> : ObjectCacheProvider<TConfig, TConnector>, ILogSubject
    where TConfig : class, IConnectorSettings
    where TConnector : IConnectorBase
{
    public ILogger Logger { get; }
    private readonly IServiceProvider _sp;
    private readonly ConcurrentDictionary<TConfig, Entry> _scopes = new();

    public ConnectorCacheProvider(IServiceProvider sp, ILogger logger)
    {
        Logger = logger;
        _sp = sp;
    }

    public override async Task<OneOf<TConnector, IDisposableReference<TConnector>>> CreateAsync(
        TConfig key,
        CancellationToken ct
    )
    {
        var providerKey = key.GetProviderKey();

        this.Trace("{key} - resolve entry for {config}", providerKey, key);
        var entry = _scopes.GetOrAdd(key, CreateEntry);

        this.Trace("{key} - init {key} connector for {config}", providerKey, key);
        await entry.Connector.InitAsync(); // this must not be called twice by design

        return entry.Connector;
    }

    public override async Task DisposeAsync(TConfig key, TConnector value)
    {
        var providerKey = key.GetProviderKey();

        this.Trace("resolve {key} entry for {config}", providerKey, key);
        if (!_scopes.TryGetValue(key, out var entry))
        {
            this.Warn("resolved no {key} entry for {config}", providerKey, key);
            return;
        }

        this.Warn("dispose no {key} entry for {config}", providerKey, key);
        await entry.DisposeAsync();

        this.Warn("resolved no {key} entry for {config}", providerKey, key);
    }

    private Entry CreateEntry(TConfig config)
    {
        var providerKey = ProviderKey.Create(config.Provider, config.Environment);

        this.Trace("create new {key} scope for {config}", providerKey, config);
        var scope = _sp.CreateAsyncScope();

        this.Trace("{key} - provide {config} into scope", providerKey, config);
        var injected = scope.ServiceProvider.Resolve<Injected<TConfig>>();
        injected.Init(config);

        this.Trace("create new {key} connector for {config}", providerKey, config);
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
