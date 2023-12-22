using System;
using Annium.Finance.Providers.Abstractions.Domain.Enums;

namespace Annium.Finance.Providers.Tests.Lib.Models;

public sealed record Resource(Guid Id, string Provider, ProviderEnvironment Environment, string Code, byte Precision);
