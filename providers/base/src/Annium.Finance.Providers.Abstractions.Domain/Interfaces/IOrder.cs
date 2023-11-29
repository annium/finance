using System;
using Annium.Finance.Providers.Abstractions.Domain.Enums;

namespace Annium.Finance.Providers.Abstractions.Domain.Interfaces;

public interface IOrder<TPosition, TInstrument, TResource> : IOrder
    where TPosition : IPosition<TInstrument, TResource>
    where TInstrument : IInstrument<TResource>
    where TResource : IResource
{
    TPosition Position { get; }
}

public interface IOrder : IOrderBase
{
    Guid Id { get; }
    Guid PositionId { get; }
}

public interface IOrderBase
{
    OrderSide Side { get; }
    OrderType Type { get; }
    decimal TotalQty { get; }
    decimal Price { get; }
    decimal LevelPrice { get; }
    long CreatedAt { get; }
    OrderStatus Status { get; }
    decimal ExecutedQty { get; }
    decimal ExecutedPrice { get; }
    decimal Fee { get; }
    long UpdatedAt { get; }
}
