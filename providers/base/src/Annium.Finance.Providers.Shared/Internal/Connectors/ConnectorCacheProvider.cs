using System;
using System.Threading;
using System.Threading.Tasks;
using Annium.Core.DependencyInjection;
using Annium.Extensions.Pooling;
using Annium.Finance.Providers.Abstractions.Connectors.Connectors;
using Annium.Finance.Providers.Abstractions.Domain.Interfaces;
using Annium.Finance.Providers.Abstractions.Domain.Models;
using Annium.Logging;
using OneOf;

namespace Annium.Finance.Providers.Shared.Internal.Connectors;

internal class ConnectorCacheProvider<TConfig, TConnector> : ObjectCacheProvider<TConfig, TConnector>, ILogSubject
    where TConfig : IConnectorConfig
    where TConnector : IConnectorBase<TConfig>
{
    public ILogger Logger { get; }
    private readonly IIndex<ProviderKey, Func<TConnector>> _connectorFactories;

    public ConnectorCacheProvider(IIndex<ProviderKey, Func<TConnector>> connectorFactories, ILogger logger)
    {
        Logger = logger;
        _connectorFactories = connectorFactories;
    }

    public override async Task<OneOf<TConnector, IDisposableReference<TConnector>>> CreateAsync(
        TConfig config,
        CancellationToken ct
    )
    {
        var provider = config.Provider;
        var env = config.Environment;
        var providerKey = ProviderKey.Create(provider, env);

        this.Trace("create new {key} connector for {config}", providerKey, config);
        var connector = _connectorFactories[providerKey]();

        this.Trace("init {key} connector for {config}", providerKey, config);
        await connector.InitAsync(config);

        return connector;
    }
}
