using System.Text;
using Annium.Finance.Providers.Core;
using Annium.Finance.Providers.Crypto.Binance.Base.Shared.Contracts.Domain;
using Annium.Finance.Providers.Tests.Lib;
using Annium.Finance.Providers.Tests.Lib.Infrastructure;
using Annium.Testing;
using Xunit;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Tests.Internal.Shared.Contracts.Converters;

/// <summary>
/// Verifies that <c>ServerTimeConverter</c> reads Binance's <c>{serverTime}</c> response into a
/// <see cref="ServerTime"/>, and that a payload under a different field name deserializes to null instead
/// of throwing.
/// </summary>
public class ServerTimeConverterTests : ProvidersTestBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ServerTimeConverterTests"/> class.
    /// </summary>
    /// <param name="outputHelper">The xUnit output helper to route trace logging to.</param>
    public ServerTimeConverterTests(ITestOutputHelper outputHelper)
        : base(outputHelper) { }

    /// <summary>
    /// Registers the Binance USD-M futures provider so the converter under test is resolved from its actual registration.
    /// </summary>
    /// <param name="ctx">The fluent context to register providers into.</param>
    protected override void RegisterProvider(ProviderRegistrationContext ctx)
    {
        ctx.WithBinanceUsdFutures();
    }

    /// <summary>
    /// A <c>{serverTime}</c> payload is parsed into its millisecond value.
    /// </summary>
    [Fact]
    public void Works()
    {
        // arrange
        var raw =
            @"{
            ""serverTime"":123
        }";

        // act - deserialize
        var serializer = this.GetJsonSerializer(Constants.ServerTimeKey);
        var deserialized = serializer.Deserialize<ServerTime?>(Encoding.UTF8.GetBytes(raw)).NotNull();

        // assert - deserialization
        deserialized.Value.Is(123);
    }

    /// <summary>
    /// A payload carrying the value under a different field name (<c>time</c> instead of <c>serverTime</c>)
    /// deserializes to null instead of throwing.
    /// </summary>
    [Fact]
    public void InvalidDataReturnsEmpty()
    {
        // arrange
        var raw =
            @"{
            ""time"":123
        }";

        // act - deserialize
        var serializer = this.GetJsonSerializer(Constants.ServerTimeKey);
        var deserialized = serializer.Deserialize<ServerTime?>(Encoding.UTF8.GetBytes(raw));

        // assert - deserialization
        deserialized.IsDefault();
    }
}
