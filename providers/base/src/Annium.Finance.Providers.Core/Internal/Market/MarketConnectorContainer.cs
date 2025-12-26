using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Annium.Finance.Providers.Abstractions.Connectors.Market;
using Annium.Finance.Providers.Abstractions.Connectors.Shared;
using Annium.Finance.Providers.Abstractions.Domain.Market;
using Annium.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace Annium.Finance.Providers.Core.Internal.Market;

internal class MarketConnectorContainer : IMarketConnector, ILogSubject
{
    public ILogger Logger { get; }
    public ConnectorStatus Status => _connector.Status;
    public IReadOnlyCollection<ResourceModel> Resources => _connector.Resources;
    public IReadOnlyCollection<InstrumentModel> Instruments => _connector.Instruments;
    public IObservable<InstrumentTicker> Tickers => _connector.Tickers;

    public event Action<ConnectorStatus> OnStatusChanged = delegate { };
    public event Action<ConnectorError> OnError = delegate { };
    public event Func<
        MarketSettings,
        IReadOnlyCollection<ResourceModel>,
        IReadOnlyCollection<InstrumentModel>,
        Task
    > OnSync = delegate
    {
        return Task.CompletedTask;
    };

    private readonly AsyncServiceScope _scope;
    private readonly IMarketConnector _connector;
    private readonly DisposableBox _disposable;

    public MarketConnectorContainer(AsyncServiceScope scope, IMarketConnector connector, ILogger logger)
    {
        Logger = logger;
        _scope = scope;
        _connector = connector;
        _disposable = Disposable.Box(logger);

        _connector.OnStatusChanged += HandleStatusChanged;
        _disposable += () => _connector.OnStatusChanged -= HandleStatusChanged;

        _connector.OnError += HandleError;
        _disposable += () => _connector.OnError -= HandleError;

        _connector.OnSync += HandleSyncAsync;
        _disposable += () => _connector.OnSync -= HandleSyncAsync;
    }

    public async ValueTask DisposeAsync()
    {
        this.Trace("dispose connector bindings");
        _disposable.Dispose();

        this.Trace("dispose connector");
        await _connector.DisposeAsync();

        this.Trace("dispose scope");
        await _scope.DisposeAsync();

        this.Trace("done");
    }

    public void Sync()
    {
        _connector.Sync();
    }

    public void SubscribeTickers(IReadOnlyCollection<string> symbols)
    {
        _connector.SubscribeTickers(symbols);
    }

    public void UnsubscribeTickers(IReadOnlyCollection<string> symbols)
    {
        _connector.UnsubscribeTickers(symbols);
    }

    private void HandleStatusChanged(ConnectorStatus status) => OnStatusChanged(status);

    private void HandleError(ConnectorError error) => OnError(error);

    private Task HandleSyncAsync(
        MarketSettings settings,
        IReadOnlyCollection<ResourceModel> resources,
        IReadOnlyCollection<InstrumentModel> instruments
    ) => OnSync(settings, resources, instruments);
}
