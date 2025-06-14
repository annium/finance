using System.Text;
using Annium.Finance.Providers.Crypto.Binance.Base.Contracts.Shared.Domain;
using Annium.Finance.Providers.Tests.Shared.Connectors;
using Annium.Finance.Providers.Tests.Shared.Extensions;
using Annium.Testing;
using Xunit;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Tests.Internal.Contracts.Shared.Converters;

public class OperationResultConverterTests : ConnectorTestBase
{
    public OperationResultConverterTests(ITestOutputHelper outputHelper)
        : base(ctx => ctx.WithBinanceUsdFutures(), outputHelper) { }

    [Fact]
    public void Works()
    {
        // arrange
        var raw = @"{""code"":-1121,""msg"":""smth bad""}";

        // act - deserialize
        var serializer = this.GetJsonSerializer(Constants.InitOrderKey);
        var deserialized = serializer.Deserialize<OperationResult>(Encoding.UTF8.GetBytes(raw)).NotNull();

        // assert
        deserialized.Code.Is(-1121);
        deserialized.Message.Is("smth bad");
    }

    [Fact]
    public void InvalidDataReturnsEmpty()
    {
        // arrange
        var raw = @"{""msg"":""smth bad""}";

        // act - deserialize
        var serializer = this.GetJsonSerializer(Constants.InitOrderKey);
        var deserialized = serializer.Deserialize<OperationResult>(Encoding.UTF8.GetBytes(raw));

        // assert - deserialization
        deserialized.IsDefault();
    }
}
