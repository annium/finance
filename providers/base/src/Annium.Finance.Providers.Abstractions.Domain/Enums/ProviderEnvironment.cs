using System;
using Annium.Core.Mapper.Attributes;

namespace Annium.Finance.Providers.Abstractions.Domain.Enums;

[Flags]
[AutoMapped]
public enum ProviderEnvironment
{
    Real = 1 << 0,
    Test = 1 << 1,
}
