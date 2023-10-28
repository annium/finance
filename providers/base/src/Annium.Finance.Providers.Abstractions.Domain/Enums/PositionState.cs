using Annium.Core.Mapper.Attributes;

namespace Annium.Finance.Providers.Abstractions.Domain.Enums;

[AutoMapped]
public enum PositionState
{
    Blank,
    Opening,
    Opened,
    Closing,
    Closed,
    Canceled,
}
