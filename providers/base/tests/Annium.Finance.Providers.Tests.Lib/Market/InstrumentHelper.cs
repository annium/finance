using Annium.Finance.Providers.Abstractions.Domain.Shared;

namespace Annium.Finance.Providers.Tests.Lib.Market;

public static class InstrumentHelper
{
    public static readonly Instrument DefaultInstrument = CreateInstrument("BTC", "USDT", 0.1m, 0.01m);

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
