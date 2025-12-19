using System;
using Annium.Finance.Providers.Abstractions.Domain.Shared;

namespace Annium.Finance.Providers.Tests.Lib.Market;

public sealed record Resource(Guid Id, string Provider, ProviderEnvironment Environment, string Code, byte Precision);
