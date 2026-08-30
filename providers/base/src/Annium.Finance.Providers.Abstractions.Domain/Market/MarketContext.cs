using System.Collections.Generic;

namespace Annium.Finance.Providers.Abstractions.Domain.Market;

/// <summary>
/// Represents the full set of resources and instruments available from a market data provider.
/// </summary>
/// <param name="Resources">The resources (assets, currencies) known to the provider.</param>
/// <param name="Instruments">The tradable instruments known to the provider.</param>
public sealed record MarketContext(
    IReadOnlyCollection<ResourceModel> Resources,
    IReadOnlyCollection<InstrumentModel> Instruments
);
