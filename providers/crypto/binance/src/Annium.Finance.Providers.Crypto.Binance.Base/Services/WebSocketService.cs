using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using Annium.Finance.Providers.Crypto.Binance.Base.Connectors;
using Annium.Finance.Providers.Shared.Connectors;
using Annium.Logging;
using Annium.Net.WebSockets;

namespace Annium.Finance.Providers.Crypto.Binance.Base.Services;

public abstract class WebSocketService : IDisposable, ILogSubject
{
    public ILogger Logger { get; }
    private readonly IClientWebSocket _socket;
    private readonly HashSet<string> _topics = new();
    private readonly IStatusReporter _statusReporter;

    protected WebSocketService(BaseSettings settings, IStatusReporter statusReporter, ILogger logger)
    {
        Logger = logger;

        _socket = new ClientWebSocket(ClientWebSocketOptions.Default, logger);
        _socket.OnConnected += HandleConnected;
        _socket.OnDisconnected += HandleDisconnected;
        _socket.OnTextReceived += HandleData;

        _statusReporter = statusReporter;
        _statusReporter.Bind(this);

        _socket.Connect(new Uri(settings.WsApi, settings.WsMarketEndpoint));
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
        var targets = new List<string>(topics.Where(topic => _topics.Add(topic)));
        var request = new Request { Method = "SUBSCRIBE", Params = targets };

        _socket.SendTextAsync(JsonSerializer.SerializeToUtf8Bytes(request));
    }

    protected void UnsubscribeTopics(IEnumerable<string> topics)
    {
        var targets = new List<string>(topics.Where(topic => _topics.Remove(topic)));
        var request = new Request { Method = "UNSUBSCRIBE", Params = targets };

        _socket.SendTextAsync(JsonSerializer.SerializeToUtf8Bytes(request));
    }

    protected abstract void HandleData(ReadOnlyMemory<byte> raw);

    private void HandleConnected()
    {
        this.Trace("connected");
        _statusReporter.Connected();
    }

    private void HandleDisconnected(WebSocketCloseStatus status)
    {
        this.Trace("disconnected: {status}", status);
        _statusReporter.Connecting();
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
