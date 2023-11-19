using System.Collections.Generic;
using Annium.Finance.Providers.Abstractions.Domain.Dto;

namespace Annium.Finance.Providers.Crypto.Binance.Base.Connectors;

public sealed record MarketContext(
    IReadOnlyCollection<ResourceDto> Resources,
    IReadOnlyCollection<InstrumentDto> Instruments
);
