using System;
using Annium.Finance.Providers.Abstractions.Domain.Shared;

namespace Annium.Finance.Providers.Tests.Lib.Market;

/// <summary>
/// A fake tradable asset (base, quote or currency) used to build test instruments and positions.
/// </summary>
/// <param name="Id">The resource's unique identifier.</param>
/// <param name="Provider">The name of the (fake) provider the resource belongs to.</param>
/// <param name="Environment">The environment the resource is registered for.</param>
/// <param name="Code">The resource's asset code (e.g. "BTC").</param>
/// <param name="Precision">The number of decimal digits amounts of this resource are reported with.</param>
public sealed record Resource(Guid Id, string Provider, ProviderEnvironment Environment, string Code, byte Precision);
