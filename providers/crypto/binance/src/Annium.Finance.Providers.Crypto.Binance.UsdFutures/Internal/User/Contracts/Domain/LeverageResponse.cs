namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.User.Contracts.Domain;

/// <summary>
/// The response of the change-leverage endpoint (<c>POST /fapi/v1/leverage</c>), confirming the leverage now in
/// effect for the requested symbol.
/// </summary>
/// <param name="Leverage">The leverage now in effect.</param>
public sealed record LeverageResponse(decimal Leverage);
