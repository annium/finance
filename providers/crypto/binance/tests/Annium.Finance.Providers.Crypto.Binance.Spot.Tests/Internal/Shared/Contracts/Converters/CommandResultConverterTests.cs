using System.Text;
using Annium.Finance.Providers.Core;
using Annium.Finance.Providers.Crypto.Binance.Base.Shared.Contracts.Domain;
using Annium.Finance.Providers.Tests.Lib;
using Annium.Finance.Providers.Tests.Lib.Infrastructure;
using Annium.Testing;
using Xunit;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Tests.Internal.Shared.Contracts.Converters;

/// <summary>
/// Verifies that <c>CommandResultConverter</c> reads the <c>{id, result}</c> envelope Binance wraps
/// WebSocket API command responses in, pulling out just the correlation id, and that a payload without
/// an <c>id</c> field deserializes to null instead of throwing.
/// </summary>
public class CommandResultConverterTests : ProvidersTestBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CommandResultConverterTests"/> class.
    /// </summary>
    /// <param name="outputHelper">The xUnit output helper to route trace logging to.</param>
    public CommandResultConverterTests(ITestOutputHelper outputHelper)
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
    /// A command-result envelope with a null result is parsed for its correlation id.
    /// </summary>
    [Fact]
    public void Works()
    {
        // arrange
        var raw = @"{""id"":1,""result"":null}";

        // act - deserialize
        var serializer = this.GetJsonSerializer(Constants.InstrumentTickerKey);
        var deserialized = serializer.Deserialize<CommandResult>(Encoding.UTF8.GetBytes(raw)).NotNull();

        // assert
        deserialized.Id.Is(1);
    }

    /// <summary>
    /// A payload without an <c>id</c> field deserializes to null instead of throwing.
    /// </summary>
    [Fact]
    public void InvalidDataReturnsEmpty()
    {
        // arrange
        var raw = @"{""msg"":""smth bad""}";

        // act - deserialize
        var serializer = this.GetJsonSerializer(Constants.InstrumentTickerKey);
        var deserialized = serializer.Deserialize<CommandResult>(Encoding.UTF8.GetBytes(raw));

        // assert - deserialization
        deserialized.IsDefault();
    }
}
