using Annium.Core.Mapper.Attributes;

namespace Annium.Finance.Providers.Abstractions.Domain.Enums;

[AutoMapped]
public enum MarginType
{
    Cross,
    Isolated,
}
