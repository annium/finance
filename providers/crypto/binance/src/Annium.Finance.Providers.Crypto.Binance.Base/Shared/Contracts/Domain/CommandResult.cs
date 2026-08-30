namespace Annium.Finance.Providers.Crypto.Binance.Base.Shared.Contracts.Domain;

/// <summary>Acknowledgement of a Binance WebSocket <c>SUBSCRIBE</c>/<c>UNSUBSCRIBE</c> command.</summary>
/// <param name="Id">The id of the request the acknowledgement corresponds to.</param>
public sealed record CommandResult(long Id);
