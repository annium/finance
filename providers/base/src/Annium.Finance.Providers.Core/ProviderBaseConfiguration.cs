using Annium.Finance.Providers.Abstractions.Domain.Shared;
using Annium.Finance.Providers.Core.Shared.TimeSync;

namespace Annium.Finance.Providers.Core;

public sealed record ProviderBaseConfiguration(
    string Provider,
    ProviderEnvironment Environments,
    ServerTimeProviderConfig ServerTime
);
