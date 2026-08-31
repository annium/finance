using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using Annium.Finance.Providers.Abstractions.Connectors.Shared;
using Annium.Finance.Providers.Core.Shared.Status;
using Annium.Finance.Providers.Crypto.Binance.Base.Market;
using Annium.Linq;
using Annium.Logging;
using Annium.Net.WebSockets;

namespace Annium.Finance.Providers.Crypto.Binance.Base.Internal.Market.Services;

/// <summary>
/// Base class for market-data services that subscribe to Binance's public WebSocket stream, tracking the currently
/// subscribed topics and issuing <c>SUBSCRIBE</c>/<c>UNSUBSCRIBE</c> requests as needed.
/// </summary>
internal abstract class WebSocketService : IDisposable, ILogSubject
{
    /// <summary>Gets the logger used to trace connection and subscription activity.</summary>
    public ILogger Logger { get; }

    /// <summary>The underlying WebSocket client connected to Binance's market data stream.</summary>
    private readonly IClientWebSocket _socket;

    /// <summary>The set of topics currently subscribed to, re-sent on reconnect.</summary>
    private readonly HashSet<string> _topics = new();

    /// <summary>The reporter used to publish connection status changes.</summary>
    private readonly IStatusReporter _statusReporter;

    /// <summary>Initializes a new instance of the <see cref="WebSocketService"/> class and starts connecting to the market WebSocket API.</summary>
    /// <param name="config">The market configuration providing the WebSocket API endpoint.</param>
    /// <param name="statusReporter">The reporter used to publish connection status changes.</param>
    /// <param name="logger">The logger to trace connection and subscription activity with.</param>
    protected WebSocketService(MarketConfigBase config, IStatusReporter statusReporter, ILogger logger)
    {
        Logger = logger;

        _socket = new ClientWebSocket(ClientWebSocketOptions.Default, logger);
        _socket.OnConnected += HandleConnected;
        _socket.OnDisconnected += HandleDisconnected;
        _socket.OnTextReceived += HandleData;
        // the user stream reports its socket's faults and this half never did, so a market connector that
        // kept failing looked no different from one merely reconnecting - and OnError is the only channel
        // a connector has for saying otherwise
        _socket.OnError += HandleError;

        _statusReporter = statusReporter;
        _statusReporter.Bind(this);

        _socket.Connect(new Uri(config.WsApi, config.WsUriPath));
        _statusReporter.Connecting();
    }

    /// <summary>Disconnects and disposes the underlying WebSocket, reporting the connector as disconnected.</summary>
    public void Dispose()
    {
        this.Trace("start");

        this.Trace("signal disconnected");
        _statusReporter.Disconnected();

        // and stop counting: a disposed component is gone, not disconnected. Left registered, it sits
        // in the monitor as a disconnected target beside the live ones, and the connector can never
        // report itself connected again for as long as it lives
        _statusReporter.Unbind();

        this.Trace("dispose socket");
        _socket.Dispose();

        this.Trace("done");
    }

    /// <summary>Adds the given topics to the tracked subscription set and sends a <c>SUBSCRIBE</c> request for the ones not already subscribed to.</summary>
    /// <param name="topics">The topic names to subscribe to.</param>
    protected void SubscribeTopics(IEnumerable<string> topics)
    {
        this.Trace("start");

        var targets = new List<string>(topics.Where(topic => _topics.Add(topic)));
        if (targets.Count == 0)
        {
            this.Trace("skip - no topics to subscribe");
            return;
        }

        this.Trace<string>("subscribe to {topics}", targets.Join(","));
        var request = new Request { Method = "SUBSCRIBE", Params = targets };
        _socket.SendTextAsync(JsonSerializer.SerializeToUtf8Bytes(request)).GetAwaiter();

        this.Trace("done");
    }

    /// <summary>Removes the given topics from the tracked subscription set and sends an <c>UNSUBSCRIBE</c> request for the ones that were subscribed to.</summary>
    /// <param name="topics">The topic names to unsubscribe from.</param>
    protected void UnsubscribeTopics(IEnumerable<string> topics)
    {
        this.Trace("start");

        var targets = new List<string>(topics.Where(topic => _topics.Remove(topic)));
        if (targets.Count == 0)
        {
            this.Trace("skip - no topics to unsubscribe");
            return;
        }

        this.Trace<string>("unsubscribe from {topics}", targets.Join(","));
        var request = new Request { Method = "UNSUBSCRIBE", Params = targets };
        _socket.SendTextAsync(JsonSerializer.SerializeToUtf8Bytes(request)).GetAwaiter();

        this.Trace("done");
    }

    /// <summary>Handles a raw text message received over the WebSocket, deserializing and dispatching it to derived-class subscribers.</summary>
    /// <param name="raw">The raw UTF-8 text payload received over the WebSocket.</param>
    protected abstract void HandleData(ReadOnlyMemory<byte> raw);

    /// <summary>Reports the connector as connected and re-sends a <c>SUBSCRIBE</c> request for all currently tracked topics.</summary>
    private void HandleConnected()
    {
        this.Trace("start");

        this.Trace("signal connected");
        _statusReporter.Connected();

        if (_topics.Count == 0)
        {
            this.Trace("skip - no topics to subscribe");
            return;
        }

        this.Trace<string>("subscribe to {topics}", _topics.Join(","));
        var request = new Request { Method = "SUBSCRIBE", Params = _topics };
        _socket.SendTextAsync(JsonSerializer.SerializeToUtf8Bytes(request)).GetAwaiter();

        this.Trace("done");
    }

    /// <summary>Reports the connector as reconnecting after the WebSocket closes.</summary>
    /// <param name="status">The close status reported by the WebSocket.</param>
    private void HandleDisconnected(WebSocketCloseStatus status)
    {
        this.Trace("start");

        // a close that carries an error is not an ordinary reconnect, and saying only "connecting" loses
        // the difference - the same distinction the user stream already draws
        if (status is WebSocketCloseStatus.Error)
            _statusReporter.Error(new ConnectorError("WebSocket closed with error"));

        this.Trace("signal disconnected: {status}", status);
        _statusReporter.Connecting();

        this.Trace("done");
    }

    /// <summary>Reports a WebSocket error and switches the connector back to reconnecting.</summary>
    /// <param name="error">The exception raised by the WebSocket.</param>
    private void HandleError(Exception error)
    {
        this.Trace("start");

        _statusReporter.Error(new ConnectorError(error.ToString()));
        _statusReporter.Connecting();

        this.Trace("done");
    }

    /// <summary>The JSON payload of a Binance WebSocket <c>SUBSCRIBE</c>/<c>UNSUBSCRIBE</c> control request.</summary>
    private record Request
    {
        /// <summary>The last request id issued, used to hand out unique, monotonically increasing ids.</summary>
        private static long _lastId;

        /// <summary>Gets a new unique id for this request, assigned on every read.</summary>
        [JsonPropertyName("id")]
        public long Id => Interlocked.Increment(ref _lastId);

        /// <summary>Gets the control method, either <c>SUBSCRIBE</c> or <c>UNSUBSCRIBE</c>.</summary>
        [JsonPropertyName("method")]
        public required string Method { get; init; }

        /// <summary>Gets the topic names the request applies to.</summary>
        [JsonPropertyName("params")]
        public required IReadOnlyCollection<string> Params { get; init; }
    }
}
