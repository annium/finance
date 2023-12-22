using System.Text;
using Annium.Finance.Providers.Crypto.Binance.UsdFutures.Internal.Contracts.User.Domain;
using Annium.Finance.Providers.Tests.Shared.Connectors;
using Annium.Finance.Providers.Tests.Shared.Extensions;
using Annium.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Tests.Internal.Contracts.User.Converters;

public class LeverageResponseConverterTests : ConnectorTestBase
{
    public LeverageResponseConverterTests(ITestOutputHelper outputHelper)
        : base(ctx => ctx.WithBinanceUsdFutures(), outputHelper) { }

    [Fact]
    public void Works()
    {
        // arrange
        var raw =
            @"{
            ""leverage"":21
        }";

        // act - deserialize
        var serializer = this.GetJsonSerializer(Constants.SetLeverageKey);
        var deserialized = serializer.Deserialize<LeverageResponse>(Encoding.UTF8.GetBytes(raw)).NotNull();

        // assert - deserialization
        deserialized.Leverage.Is(21);
    }
}
