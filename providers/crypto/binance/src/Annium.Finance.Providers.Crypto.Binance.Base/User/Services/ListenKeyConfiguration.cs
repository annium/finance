namespace Annium.Finance.Providers.Crypto.Binance.Base.User.Services;

/// <summary>The polling intervals the <see cref="IListenKeyResolver"/> uses to fetch and keep a Binance listen key alive.</summary>
/// <param name="FetchInterval">The interval, in milliseconds, between retries while fetching a new listen key.</param>
/// <param name="ConfirmInterval">The interval, in milliseconds, between keep-alive (PUT) confirmations of the current listen key.</param>
public sealed record ListenKeyConfiguration(int FetchInterval, int ConfirmInterval);
