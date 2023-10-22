using System;
using Annium.Finance.Providers.Abstractions.Domain.Interfaces;
using NodaTime;

namespace Annium.Finance.Providers.Tests.Lib.Models;

public sealed record Candle(
    Guid Id,
    Instrument Instrument,
    Instant Moment,
    decimal Open,
    decimal High,
    decimal Low,
    decimal Close,
    decimal Volume
) : ICandle<Instrument, Resource>
{
    public Guid InstrumentId { get; } = Instrument.Id;
}