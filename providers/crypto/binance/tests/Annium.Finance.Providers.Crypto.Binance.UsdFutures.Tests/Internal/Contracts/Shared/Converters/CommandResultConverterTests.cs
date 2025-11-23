using System.Text;
using Annium.Finance.Providers.Crypto.Binance.Base.Contracts.Shared.Domain;
using Annium.Finance.Providers.Tests.Lib;
using Annium.Finance.Providers.Tests.Lib.Extensions;
using Annium.Testing;
using Xunit;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Tests.Internal.Contracts.Shared.Converters;

public class CommandResultConverterTests : ProvidersTestBase
{
    public CommandResultConverterTests(ITestOutputHelper outputHelper)
        : base(ctx => ctx.WithBinanceUsdFutures(), outputHelper) { }

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
