using System;
using System.Collections;
using System.Collections.Generic;
using Annium.Finance.Providers.Abstractions.Domain.Enums;
using Annium.Finance.Providers.Abstractions.Domain.Models;

namespace Annium.Finance.Providers.Tests.Shared.Connectors;

public abstract record ProviderEnvironmentsBase : IEnumerable<object[]>
{
    private readonly List<object[]> _keys = [];

    protected ProviderEnvironmentsBase(string provider, ProviderEnvironment env)
    {
        foreach (var option in Enum.GetValues<ProviderEnvironment>())
            if (env.HasFlag(option))
                _keys.Add([ProviderKey.Create(provider, option)]);
    }

    public IEnumerator<object[]> GetEnumerator() => _keys.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
