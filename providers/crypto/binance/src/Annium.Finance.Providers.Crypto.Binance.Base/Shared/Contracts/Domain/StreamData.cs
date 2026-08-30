namespace Annium.Finance.Providers.Crypto.Binance.Base.Shared.Contracts.Domain;

/// <summary>The envelope Binance wraps combined-stream WebSocket payloads in.</summary>
/// <typeparam name="T">The type of the wrapped payload.</typeparam>
/// <param name="Name">The name of the stream/topic the payload was received on, e.g. <c>btcusdt@bookTicker</c>.</param>
/// <param name="Data">The wrapped payload.</param>
public sealed record StreamData<T>(string Name, T Data);
