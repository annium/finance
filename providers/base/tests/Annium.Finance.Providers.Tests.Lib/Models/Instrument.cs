using Annium.Finance.Providers.Abstractions.Domain.Market;
using Annium.Finance.Providers.Abstractions.Domain.Shared;
using Annium.Finance.Providers.Abstractions.Domain.User;

namespace Annium.Finance.Providers.Tests.Lib.Models;

public sealed record Instrument(
    string Provider,
    ProviderEnvironment Environment,
    string Symbol,
    decimal LotSize,
    decimal TickSize,
    decimal MinQty,
    decimal MaxQty,
    decimal MinSum,
    decimal MaxSum,
    int MaxOrders
) : IInstrument
{
    public override string ToString() => Symbol;
}
