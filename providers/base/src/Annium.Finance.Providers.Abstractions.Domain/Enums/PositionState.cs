using System;
using Annium.Core.Mapper.Attributes;

namespace Annium.Finance.Providers.Abstractions.Domain.Enums;

[AutoMapped]
[Flags]
public enum PositionState
{
    Blank = 1 << 0,
    Opening = 1 << 1,
    Opened = 1 << 2,
    Closing = 1 << 3,
    Closed = 1 << 4,
    Filled = 1 << 5,
    Canceled = 1 << 6,
}
