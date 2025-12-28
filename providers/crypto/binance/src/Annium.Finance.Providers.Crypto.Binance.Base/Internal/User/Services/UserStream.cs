using System;
using Annium.Finance.Providers.Abstractions.Connectors.Shared;
using Annium.Finance.Providers.Core.Shared.Status;
using Annium.Finance.Providers.Crypto.Binance.Base.User;
using Annium.Finance.Providers.Crypto.Binance.Base.User.Services;
using Annium.Logging;
using Annium.Net.WebSockets;

namespace Annium.Finance.Providers.Crypto.Binance.Base.Internal.User.Services;

internal class UserStream : IUserStream, ILogSubject
{
    public ILogger Logger { get; }
    public event Action OnConnected = delegate { };
    public event Action OnDisconnected = delegate { };
    public event Action<ReadOnlyMemory<byte>> OnMessage = delegate { };
    private readonly UserConfigBase _config;
    private readonly IListenKeyResolver _listenKeyResolver;
    private readonly IStatusReporter _statusReporter;
    private readonly ClientWebSocket _ws;
    private readonly DisposableBox _disposable;

    public UserStream(
        UserConfigBase config,
        IListenKeyResolver listenKeyResolver,
        IStatusReporter statusReporter,
        ILogger logger
    )
    {
        Logger = logger;
        _config = config;
        _listenKeyResolver = listenKeyResolver;
        _statusReporter = statusReporter;
        _statusReporter.Bind(this);
        _statusReporter.Connecting();

        _disposable = Disposable.Box(logger);

        // subscribe listen key resolver
        _listenKeyResolver.OnListenKeyFetched += StartConnection;
        _disposable += () => _listenKeyResolver.OnListenKeyFetched -= StartConnection;

        _listenKeyResolver.OnListenKeyReset += StopConnection;
        _disposable += () => _listenKeyResolver.OnListenKeyReset -= StopConnection;

        // subscribe WebSocket
        _disposable += _ws = new ClientWebSocket(logger);

        _ws.OnConnected += HandleConnected;
        _disposable += () => _ws.OnConnected -= HandleConnected;

        _ws.OnDisconnected += HandleDisconnected;
        _disposable += () => _ws.OnDisconnected -= HandleDisconnected;

        _ws.OnTextReceived += HandleMessage;
        _disposable += () => _ws.OnTextReceived -= HandleMessage;

        _ws.OnError += HandleError;
        _disposable += () => _ws.OnError -= HandleError;
    }

    public void Dispose()
    {
        this.Trace("start");

        _disposable.Dispose();
        _statusReporter.Disconnected();

        this.Trace("done");
    }

    private void StartConnection(string listenKey)
    {
        this.Trace("start");

        _statusReporter.Connecting();

        var uri = new Uri(_config.WsApi, _config.ListenKeyUriPath + listenKey);
        _ws.Connect(uri);

        this.Trace("done");
    }

    private void StopConnection()
    {
        this.Trace("start");

        _statusReporter.Connecting();
        _ws.Disconnect();

        this.Trace("done");
    }

    private void HandleConnected()
    {
        // is socket was disconnected manually - ignore open event
        if (_disposable.IsDisposed)
        {
            this.Trace("skip, not connected");
            return;
        }

        this.Trace("start");

        OnConnected();
        _statusReporter.Connected();

        this.Trace("done");
    }

    private void HandleDisconnected(WebSocketCloseStatus status)
    {
        // is socket was disconnected manually - ignore close event
        if (_disposable.IsDisposed)
        {
            this.Trace("skip, not connected");
            return;
        }

        this.Trace("start");

        // request new key in ListenKeyResolver
        this.Trace("request new key");
        _listenKeyResolver.RequestNewListenKey();

        if (status is WebSocketCloseStatus.Error)
            _statusReporter.Error(new ConnectorError("WebSocket closed with error"));

        _statusReporter.Connecting();
        OnDisconnected();

        this.Trace("done");
    }

    private void HandleMessage(ReadOnlyMemory<byte> raw) => OnMessage(raw);

    private void HandleError(Exception error)
    {
        this.Trace("start");

        _statusReporter.Error(new ConnectorError(error.ToString()));
        _statusReporter.Connecting();

        this.Trace("done");
    }
}
