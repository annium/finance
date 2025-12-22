using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Annium.Core.DependencyInjection;
using Annium.Extensions.Pooling;
using Annium.Finance.Providers.Abstractions.Connectors.Shared;
using Annium.Finance.Providers.Abstractions.Domain.Shared;
using Annium.Logging;
using Microsoft.Extensions.DependencyInjection;
using OneOf;

namespace Annium.Finance.Providers.Core.Internal.Connectors;

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
        TSettings settings,
        CancellationToken ct
    )
    {
        var providerKey = settings.GetProviderKey();

        this.Trace("{key} - resolve entry for {settings}", providerKey, settings);
        var entry = _scopes.GetOrAdd(settings, CreateEntry);

        // this.Trace("{key} - init connector for {settings}", providerKey, settings);
        // await entry.Connector.InitAsync(); // this must not be called twice by design

        this.Trace("{key} - created for {settings}", providerKey, settings);

        return entry.Connector;
    }

    public override async Task DisposeAsync(TSettings settings, TConnector value)
    {
        var providerKey = settings.GetProviderKey();

        this.Trace("{key} - resolve entry for {settings}", providerKey, settings);
        if (!_scopes.TryGetValue(settings, out var entry))
        {
            this.Warn("{key} - resolved no entry for {settings}", providerKey, settings);
            return;
        }

        this.Trace("{key} - dispose entry for {settings}", providerKey, settings);
        await entry.DisposeAsync();

        this.Trace("{key} - disposed entry for {settings}", providerKey, settings);
    }

    protected abstract void Inject(IServiceProvider scopeProvider, TSettings settings);

    private Entry CreateEntry(TSettings settings)
    {
        var providerKey = ProviderKey.Create(settings.Provider, settings.Environment);

        this.Trace("{key} - create new scope for {settings}", providerKey, settings);
        var scope = _sp.CreateAsyncScope();

        this.Trace("{key} - provide {settings} into scope", providerKey, settings);
        Inject(scope.ServiceProvider, settings);

        this.Trace("{key} - create new connector for {settings}", providerKey, settings);
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
