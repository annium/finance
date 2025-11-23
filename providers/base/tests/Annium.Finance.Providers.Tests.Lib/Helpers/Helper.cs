using System;
using Annium.Finance.Providers.Abstractions.Domain.Enums;
using Annium.Finance.Providers.Tests.Lib.Models;

namespace Annium.Finance.Providers.Tests.Lib.Helpers;

public static class Helper
{
    public static readonly Instrument DefaultInstrument = CreateInstrument("XBT", "USD", 0.1m, 0.01m);

    public static Position CreatePosition(decimal leverage) => CreatePosition(DefaultInstrument, leverage);

    public static Position CreatePosition(Instrument instrument, decimal leverage) =>
        new(
            Guid.NewGuid(),
            instrument,
            0,
            OrientationRange.Both,
            MarginType.Cross,
            leverage,
            0,
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
            "fake",
            ProviderEnvironment.Test,
            $"{resource}{currency}",
            lotSize,
            tickSize,
            lotSize * 10,
            lotSize * 100,
            lotSize * tickSize * 1000,
            decimal.MaxValue,
            int.MaxValue
        );
}
