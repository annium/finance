using Annium.Finance.Providers.Abstractions.Domain.Enums;

namespace Annium.Finance.Providers.Abstractions.Domain.Dto;

public sealed record ResourceDto(
    string Provider,
    ProviderEnvironment Environment,
    string Code,
    byte Precision
);