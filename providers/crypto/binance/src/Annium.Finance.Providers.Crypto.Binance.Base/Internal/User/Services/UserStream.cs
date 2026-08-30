using System;
using Annium.Finance.Providers.Abstractions.Connectors.Shared;
using Annium.Finance.Providers.Core.Shared.Status;
using Annium.Finance.Providers.Crypto.Binance.Base.User;
using Annium.Finance.Providers.Crypto.Binance.Base.User.Services;
using Annium.Logging;
using Annium.Net.WebSockets;

namespace Annium.Finance.Providers.Crypto.Binance.Base.Internal.User.Services;

/// <summary>
/// Maintains the WebSocket connection to Binance's user data stream, connecting to the URL built from the currently
/// active listen key and reconnecting whenever the <see cref="IListenKeyResolver"/> fetches or resets it.
/// </summary>
internal class UserStream : IUserStream, ILogSubject
{
    /// <summary>Gets the logger used to trace connection activity.</summary>
    public ILogger Logger { get; }

    /// <summary>Raised when the user data stream WebSocket connects.</summary>
    public event Action OnConnected = delegate { };

    /// <summary>Raised when the user data stream WebSocket disconnects.</summary>
    public event Action OnDisconnected = delegate { };

    /// <summary>Raised for every raw message received over the user data stream.</summary>
    public event Action<ReadOnlyMemory<byte>> OnMessage = delegate { };

    /// <summary>The user configuration providing the WebSocket API and listen key URI path.</summary>
    private readonly UserConfigBase _config;

    /// <summary>The resolver supplying and refreshing the listen key the stream connects with.</summary>
    private readonly IListenKeyResolver _listenKeyResolver;

    /// <summary>The reporter used to publish connection status changes.</summary>
    private readonly IStatusReporter _statusReporter;

    /// <summary>The underlying WebSocket client connected to the user data stream.</summary>
    private readonly ClientWebSocket _ws;

    /// <summary>The disposable box unsubscribing event handlers and disposing the WebSocket on teardown.</summary>
    private readonly DisposableBox _disposable;

    /// <summary>Initializes a new instance of the <see cref="UserStream"/> class and wires it to the listen key resolver's events.</summary>
    /// <param name="config">The user configuration providing the WebSocket API and listen key URI path.</param>
    /// <param name="listenKeyResolver">The resolver supplying and refreshing the listen key the stream connects with.</param>
    /// <param name="statusReporter">The reporter used to publish connection status changes.</param>
    /// <param name="logger">The logger to trace connection activity with.</param>
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

    /// <summary>Unsubscribes from the listen key resolver and WebSocket events, disposes the WebSocket, and reports the connector as disconnected.</summary>
    public void Dispose()
    {
        this.Trace("start");

        _disposable.Dispose();
        _statusReporter.Disconnected();

        this.Trace("done");
    }

    /// <summary>Connects the WebSocket to the user data stream URL built from the given listen key.</summary>
    /// <param name="listenKey">The listen key to connect the stream with.</param>
    private void StartConnection(string listenKey)
    {
        this.Trace("start");

        _statusReporter.Connecting();

        var uri = new Uri(_config.WsApi, _config.ListenKeyUriPath + listenKey);
        _ws.Connect(uri);

        this.Trace("done");
    }

    /// <summary>Disconnects the WebSocket, for example when the listen key has been reset and no longer identifies a valid stream.</summary>
    private void StopConnection()
    {
        this.Trace("start");

        _statusReporter.Connecting();
        _ws.Disconnect();

        this.Trace("done");
    }

    /// <summary>Reports the connector as connected and raises <see cref="OnConnected"/>, unless the WebSocket has already been disposed.</summary>
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

    /// <summary>Requests a new listen key, reports the connector as reconnecting, and raises <see cref="OnDisconnected"/>, unless the WebSocket has already been disposed.</summary>
    /// <param name="status">The close status reported by the WebSocket.</param>
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

    /// <summary>Forwards a raw message received over the WebSocket through <see cref="OnMessage"/>.</summary>
    /// <param name="raw">The raw UTF-8 text payload received over the WebSocket.</param>
    private void HandleMessage(ReadOnlyMemory<byte> raw) => OnMessage(raw);

    /// <summary>Reports a WebSocket error and switches the connector back to reconnecting.</summary>
    /// <param name="error">The exception raised by the WebSocket.</param>
    private void HandleError(Exception error)
    {
        this.Trace("start");

        _statusReporter.Error(new ConnectorError(error.ToString()));
        _statusReporter.Connecting();

        this.Trace("done");
    }
}
