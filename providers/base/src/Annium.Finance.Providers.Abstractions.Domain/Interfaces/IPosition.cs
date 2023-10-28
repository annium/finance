using System;
using Annium.Finance.Providers.Abstractions.Domain.Enums;

namespace Annium.Finance.Providers.Abstractions.Domain.Interfaces;

public interface IPosition<TInstrument, TResource> : IPosition
    where TInstrument : IInstrument<TResource>
    where TResource : IResource
{
    TInstrument Instrument { get; }
}

public interface IPosition
{
    Guid Id { get; }
    Guid InstrumentId { get; }
    OrientationRange OrientationRange { get; }
    MarginType MarginType { get; }
    byte Leverage { get; }
    bool IsActive { get; }
    Orientation Orientation { get; }
}
