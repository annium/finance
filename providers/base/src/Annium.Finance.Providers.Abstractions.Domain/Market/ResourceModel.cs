namespace Annium.Finance.Providers.Abstractions.Domain.Market;

/// <summary>
/// Describes an asset or currency known to a provider (e.g. "BTC", "USDT").
/// </summary>
/// <param name="Code">The resource's ticker code, as used by the provider.</param>
/// <param name="Precision">The number of decimal digits the provider reports amounts of this resource with.</param>
public sealed record ResourceModel(string Code, byte Precision);
