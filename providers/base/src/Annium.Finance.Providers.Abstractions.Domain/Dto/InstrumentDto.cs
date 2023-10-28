using Annium.Finance.Providers.Abstractions.Domain.Enums;

namespace Annium.Finance.Providers.Abstractions.Domain.Dto;

public sealed record InstrumentDto(
    string Provider,
    ProviderEnvironment Environment,
    string Symbol,
    ResourceDto Target,
    ResourceDto Quote,
    ResourceDto Currency,
    decimal LotSize,
    decimal TickSize,
    decimal MinQty,
    decimal MaxQty,
    decimal MinSum,
    decimal MaxSum,
    int MaxOrders,
    decimal MaxPosition
)
{
    public ResourceDto Target { get; private set; } = Target;
    public ResourceDto Quote { get; private set; } = Quote;
    public ResourceDto Currency { get; private set; } = Currency;

    public void Update(ResourceDto target, ResourceDto quote, ResourceDto currency)
    {
        Target = target;
        Quote = quote;
        Currency = currency;
    }
}
