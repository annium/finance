using Annium.Finance.Providers.Crypto.Binance.Base.Services;
using Annium.Finance.Providers.Tests.Shared;
using Annium.Testing;
using Xunit;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Tests.Services;

public class SignatureServiceTests : ProvidersTestBase
{
    public SignatureServiceTests(ITestOutputHelper outputHelper)
        : base(ctx => ctx.WithBinanceUsdFutures(), outputHelper)
    {
        Inject(Settings.Market);
        Inject(Settings.User);
    }

    [Fact]
    public void Signature_IsValid()
    {
        // arrange
        var service = Get<SignatureService>();
        var expectedSignature = Settings.ExpectedSignature;
        var query =
            "symbol=LTCBTC&side=BUY&type=LIMIT&timeInForce=GTC&quantity=1&price=0.1&recvWindow=5000&timestamp=1499827319559";

        // act
        var signature = service.GetSignature(query);

        // assert
        service.GetKey().Is(Settings.User.Key);
        signature.Is(expectedSignature);
    }
}
