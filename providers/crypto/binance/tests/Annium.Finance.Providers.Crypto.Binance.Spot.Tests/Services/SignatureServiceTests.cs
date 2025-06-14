using Annium.Finance.Providers.Crypto.Binance.Base.Services;
using Annium.Finance.Providers.Tests.Shared.Connectors;
using Annium.Testing;
using Xunit;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Tests.Services;

public class SignatureServiceTests : ConnectorTestBase
{
    public SignatureServiceTests(ITestOutputHelper outputHelper)
        : base(ctx => ctx.WithBinanceSpot(), outputHelper)
    {
        Inject(Markets.Test);
        Inject(Users.Test);
    }

    [Fact]
    public void Signature_IsValid()
    {
        // arrange
        var service = Get<SignatureService>();
        var expectedSignature = "28aa84f74f3f7df9ae1da32c2f9af976f8e45de5057f8dd7618d35bae9b9fd95";
        var query =
            "symbol=LTCBTC&side=BUY&type=LIMIT&timeInForce=GTC&quantity=1&price=0.1&recvWindow=5000&timestamp=1499827319559";

        // act
        var signature = service.GetSignature(query);

        // assert
        service.GetKey().Is(Users.Test.Key);
        signature.Is(expectedSignature);
    }
}
