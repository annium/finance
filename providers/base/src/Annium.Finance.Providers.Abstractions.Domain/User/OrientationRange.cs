using Annium.Core.Mapper.Attributes;

namespace Annium.Finance.Providers.Abstractions.Domain.User;

[AutoMapped]
public enum OrientationRange
{
    Both,
    Long,
    Short,
}
