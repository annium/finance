using Annium.Finance.Providers.Abstractions.Domain.Enums;
using Annium.Finance.Providers.Abstractions.Domain.Interfaces;

namespace Annium.Finance.Providers.Abstractions.Domain.Models;

public sealed record UserSettings(string Provider, ProviderEnvironment Environment, string Key, string Secret)
    : IConnectorSettings;
