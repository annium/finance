using System.Text;
using Annium.Finance.Providers.Core;
using Annium.Finance.Providers.Crypto.Binance.Base.Shared.Contracts.Domain;
using Annium.Finance.Providers.Tests.Lib;
using Annium.Finance.Providers.Tests.Lib.Infrastructure;
using Annium.Testing;
using Xunit;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Tests.Internal.Contracts.Shared.Converters;

public class OperationResultConverterTests : ProvidersTestBase
{
    public OperationResultConverterTests(ITestOutputHelper outputHelper)
        : base(outputHelper) { }

    protected override void RegisterProvider(ProviderRegistrationContext ctx)
    {
        ctx.WithBinanceUsdFutures();
    }

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
