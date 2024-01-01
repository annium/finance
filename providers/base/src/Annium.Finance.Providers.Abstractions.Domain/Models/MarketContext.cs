using System.Collections.Generic;
using Annium.Finance.Providers.Abstractions.Domain.Dto;

namespace Annium.Finance.Providers.Abstractions.Domain.Models;

public sealed record MarketContext(
    IReadOnlyCollection<ResourceDto> Resources,
    IReadOnlyCollection<InstrumentDto> Instruments
);
