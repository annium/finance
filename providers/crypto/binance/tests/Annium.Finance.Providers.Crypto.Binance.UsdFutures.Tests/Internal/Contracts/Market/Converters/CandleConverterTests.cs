using System.Collections.Generic;
using System.Text;
using Annium.Finance.Providers.Abstractions.Domain.Models;
using Annium.Finance.Providers.Tests.Lib;
using Annium.Finance.Providers.Tests.Lib.Extensions;
using Annium.Testing;
using Xunit;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Tests.Internal.Contracts.Market.Converters;

public class CandleConverterTests : ProvidersTestBase
{
    public CandleConverterTests(ITestOutputHelper outputHelper)
        : base(ctx => ctx.WithBinanceUsdFutures(), outputHelper) { }

    [Fact]
    public void Works()
    {
        // arrange
        var raw =
            @"[
          [
            1499040000000,
            ""0.01634790"",
            ""0.80000000"",
            ""0.01575800"",
            ""0.01577100"",
            ""148976.11427815""
          ]
        ]";

        // act - deserialize
        var serializer = this.GetJsonSerializer(Constants.CandleKey);
        var deserialized = serializer.Deserialize<List<CandleModel>>(Encoding.UTF8.GetBytes(raw)).NotNull();

        // assert - deserialization
        deserialized.Has(1);
        deserialized
            .At(0)
            .IsEqual(
                new CandleModel(1499040000000, 0.01634790m, 0.80000000m, 0.01575800m, 0.01577100m, 148976.11427815m)
            );
    }

    [Fact]
    public void SkipsInvalidData()
    {
        // arrange
        var raw = "[[]]";

        // act - deserialize
        var serializer = this.GetJsonSerializer(Constants.CandleKey);
        var deserialized = serializer.Deserialize<List<CandleModel>>(Encoding.UTF8.GetBytes(raw));

        // assert - deserialization
        deserialized.Has(1);
        deserialized.At(0).IsDefault();
    }
}
