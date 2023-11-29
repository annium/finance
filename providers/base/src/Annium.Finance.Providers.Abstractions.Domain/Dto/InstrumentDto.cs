using Annium.Finance.Providers.Abstractions.Domain.Interfaces;

namespace Annium.Finance.Providers.Abstractions.Domain.Dto;

public sealed record InstrumentDto(
    string Symbol,
    ResourceDto Target,
    ResourceDto Quote,
    ResourceDto Currency,
    decimal MinQty,
    decimal MaxQty,
    decimal LotSize,
    decimal MinPrice,
    decimal MaxPrice,
    decimal TickSize,
    decimal MinSum,
    decimal MaxSum,
    int MaxOrders
) : IInstrumentBase;
