using System;
using NodaTime;

namespace Annium.Finance.Providers.Abstractions.Domain.Interfaces;

public interface ICandle<TInstrument, TResource>
    where TInstrument : IInstrument<TResource>
    where TResource : IResource
{
    Guid Id { get; }
    Guid InstrumentId { get; }
    TInstrument Instrument { get; }
    Instant Moment { get; }
    decimal Open { get; }
    decimal High { get; }
    decimal Low { get; }
    decimal Close { get; }
    decimal Volume { get; }
}