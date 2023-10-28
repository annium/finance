using System;
using Annium.Finance.Providers.Abstractions.Domain.Enums;

namespace Annium.Finance.Providers.Abstractions.Domain.Interfaces;

public interface IInstrument<TResource> : IInstrument
    where TResource : IResource
{
    TResource Target { get; }
    TResource Quote { get; }
    TResource Currency { get; }
}

public interface IInstrument
{
    Guid Id { get; }
    string Provider { get; }
    ProviderEnvironment Environment { get; }
    string Symbol { get; }
    Guid TargetId { get; }
    Guid QuoteId { get; }
    Guid CurrencyId { get; }
    decimal LotSize { get; }
    decimal TickSize { get; }
    decimal MinQty { get; }
    decimal MaxQty { get; }
    decimal MinSum { get; }
    decimal MaxSum { get; }
    int MaxOrders { get; }
    decimal MaxPosition { get; }
}
