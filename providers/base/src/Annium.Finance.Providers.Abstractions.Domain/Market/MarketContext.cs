using System.Collections.Generic;

namespace Annium.Finance.Providers.Abstractions.Domain.Market;

public sealed record MarketContext(
    IReadOnlyCollection<ResourceModel> Resources,
    IReadOnlyCollection<InstrumentModel> Instruments
);
