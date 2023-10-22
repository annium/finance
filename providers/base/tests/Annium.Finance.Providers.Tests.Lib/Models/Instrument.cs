using System;
using Annium.Finance.Providers.Abstractions.Domain.Enums;
using Annium.Finance.Providers.Abstractions.Domain.Interfaces;

namespace Annium.Finance.Providers.Tests.Lib.Models;

public sealed record Instrument(
    Guid Id,
    string Provider,
    ProviderEnvironment Environment,
    string Symbol,
    Resource Target,
    Resource Quote,
    Resource Currency,
    decimal LotSize,
    decimal TickSize,
    decimal MinQty,
    decimal MaxQty,
    decimal MinSum,
    decimal MaxSum,
    int MaxOrders,
    decimal MaxPosition
) : IInstrument<Resource>
{
    public Guid TargetId { get; } = Target.Id;
    public Guid QuoteId { get; } = Quote.Id;
    public Guid CurrencyId { get; } = Currency.Id;

    public override string ToString() => Symbol;
}