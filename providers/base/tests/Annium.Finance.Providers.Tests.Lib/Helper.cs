using System;
using Annium.Finance.Providers.Abstractions.Domain.Enums;
using Annium.Finance.Providers.Tests.Lib.Models;
using NodaTime;

namespace Annium.Finance.Providers.Tests.Lib;

public static class Helper
{
    public static readonly Instrument DefaultInstrument = CreateInstrument("XBT", "USD", 0.1m, 0.01m);

    public static Position CreatePosition(byte leverage) => CreatePosition(DefaultInstrument, leverage);

    public static Position CreatePosition(Instrument instrument, byte leverage) =>
        new(
            Guid.NewGuid(),
            instrument,
            Instant.MinValue,
            OrientationRange.Both,
            MarginType.Cross,
            leverage,
            PositionState.Blank,
            Instant.MinValue,
            decimal.Zero,
            decimal.Zero,
            decimal.Zero,
            decimal.Zero,
            decimal.Zero,
            decimal.Zero,
            decimal.Zero,
            decimal.Zero,
            decimal.Zero,
            decimal.Zero,
            decimal.Zero,
            decimal.Zero
        );

    public static Instrument CreateInstrument(string resource, string currency, decimal lotSize, decimal tickSize) =>
        new(
            Guid.NewGuid(),
            "fake",
            ProviderEnvironment.Test,
            $"{resource}{currency}",
            new Resource(Guid.NewGuid(), "fake", ProviderEnvironment.Test, resource, 2),
            new Resource(Guid.NewGuid(), "fake", ProviderEnvironment.Test, currency, 2),
            new Resource(Guid.NewGuid(), "fake", ProviderEnvironment.Test, currency, 2),
            lotSize,
            tickSize,
            lotSize * 10,
            lotSize * 100,
            lotSize * tickSize * 1000,
            decimal.MaxValue,
            int.MaxValue,
            decimal.MaxValue
        );
}
