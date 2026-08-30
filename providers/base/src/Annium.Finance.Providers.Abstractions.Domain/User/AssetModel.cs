namespace Annium.Finance.Providers.Abstractions.Domain.User;

/// <summary>
/// Represents an account's balance of a single resource, split between funds available for trading and funds locked in open orders.
/// </summary>
/// <param name="Resource">The resource code the balance is denominated in.</param>
/// <param name="Free">The quantity available for trading or withdrawal.</param>
/// <param name="Locked">The quantity reserved by open orders.</param>
public sealed record AssetModel(string Resource, decimal Free, decimal Locked);
