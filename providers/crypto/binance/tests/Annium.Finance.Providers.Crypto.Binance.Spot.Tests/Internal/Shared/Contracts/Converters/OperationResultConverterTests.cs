using System.Text;
using Annium.Finance.Providers.Core;
using Annium.Finance.Providers.Crypto.Binance.Base.Shared.Contracts.Domain;
using Annium.Finance.Providers.Tests.Lib;
using Annium.Finance.Providers.Tests.Lib.Infrastructure;
using Annium.Testing;
using Xunit;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Tests.Internal.Shared.Contracts.Converters;

/// <summary>
/// Verifies that <c>OperationResultConverter</c> reads Binance's <c>{code, msg}</c> error envelope into an
/// <see cref="OperationResult"/>, and that a payload without a <c>code</c> field deserializes to null instead
/// of throwing.
/// </summary>
public class OperationResultConverterTests : ProvidersTestBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OperationResultConverterTests"/> class.
    /// </summary>
    /// <param name="outputHelper">The xUnit output helper to route trace logging to.</param>
    public OperationResultConverterTests(ITestOutputHelper outputHelper)
        : base(outputHelper) { }

    /// <summary>
    /// Registers the Binance Spot provider so the converter under test is resolved from its actual registration.
    /// </summary>
    /// <param name="ctx">The fluent context to register providers into.</param>
    protected override void RegisterProvider(ProviderRegistrationContext ctx)
    {
        ctx.WithBinanceSpot();
    }

    /// <summary>
    /// A Binance error envelope is parsed into its numeric code and message.
    /// </summary>
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

    /// <summary>
    /// A payload without a <c>code</c> field deserializes to null instead of throwing.
    /// </summary>
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
