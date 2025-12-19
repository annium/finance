using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using Annium.Finance.Providers.Core.Shared.Status;
using Annium.Linq;
using Annium.Logging;
using Annium.Net.WebSockets;

namespace Annium.Finance.Providers.Crypto.Binance.Base.Services;

public abstract class WebSocketService : IDisposable, ILogSubject
{
    public ILogger Logger { get; }
    private readonly IClientWebSocket _socket;
    private readonly HashSet<string> _topics = new();
    private readonly IStatusReporter _statusReporter;

    protected WebSocketService(MarketConfigBase config, IStatusReporter statusReporter, ILogger logger)
    {
        Logger = logger;

        _socket = new ClientWebSocket(ClientWebSocketOptions.Default, logger);
        _socket.OnConnected += HandleConnected;
        _socket.OnDisconnected += HandleDisconnected;
        _socket.OnTextReceived += HandleData;

        _statusReporter = statusReporter;
        _statusReporter.Bind(this);

        _socket.Connect(new Uri(config.WsApi, config.WsUriPath));
        _statusReporter.Connecting();
    }

    public void Dispose()
    {
        this.Trace("start");

        this.Trace("signal disconnected");
        _statusReporter.Disconnected();

        this.Trace("dispose socket");
        _socket.Dispose();

        this.Trace("done");
    }

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

    protected abstract void HandleData(ReadOnlyMemory<byte> raw);

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

    private void HandleDisconnected(WebSocketCloseStatus status)
    {
        this.Trace("start");

        this.Trace("signal disconnected: {status}", status);
        _statusReporter.Connecting();

        this.Trace("done");
    }

    private record Request
    {
        private static long _lastId;

        [JsonPropertyName("id")]
        public long Id => Interlocked.Increment(ref _lastId);

        [JsonPropertyName("method")]
        public required string Method { get; init; }

        [JsonPropertyName("params")]
        public required IReadOnlyCollection<string> Params { get; init; }
    }
}
