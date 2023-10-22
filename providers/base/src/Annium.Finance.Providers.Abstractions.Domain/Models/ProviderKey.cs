using System;
using Annium.Finance.Providers.Abstractions.Domain.Enums;

namespace Annium.Finance.Providers.Abstractions.Domain.Models;

public sealed record ProviderKey
{
    public static ProviderKey Create(string provider, ProviderEnvironment environment) => new(provider, environment);
    public string Provider { get; }
    public ProviderEnvironment Environment { get; }

    private ProviderKey(
        string provider,
        ProviderEnvironment environment
    )
    {
        Provider = provider;
        Environment = environment;
    }

    public override string ToString() => $"{Provider}[{Environment}]";

    public override int GetHashCode()
        => HashCode.Combine(Provider.GetHashCode(), (int)Environment);
}