using Annium.Finance.Providers.Abstractions.Domain.User;

namespace Annium.Finance.Providers.Abstractions.Domain.Market;

public sealed record InstrumentModel(
    string Symbol,
    ResourceModel Target,
    ResourceModel Quote,
    ResourceModel Currency,
    decimal MinQty,
    decimal MaxQty,
    decimal LotSize,
    decimal MinPrice,
    decimal MaxPrice,
    decimal TickSize,
    decimal MinSum,
    decimal MaxSum,
    int MaxOrders
) : IInstrument
{
    public override string ToString() => Symbol;
}
