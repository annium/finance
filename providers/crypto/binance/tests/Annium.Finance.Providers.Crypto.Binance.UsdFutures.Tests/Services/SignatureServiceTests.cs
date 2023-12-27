using Annium.Finance.Providers.Crypto.Binance.Base.Services;
using Annium.Finance.Providers.Tests.Shared.Connectors;
using Annium.Finance.Providers.Tests.Shared.Extensions;
using Annium.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Tests.Services;

public class SignatureServiceTests : ConnectorTestBase
{
    public SignatureServiceTests(ITestOutputHelper outputHelper)
        : base(ctx => ctx.WithBinanceUsdFutures(), outputHelper)
    {
        this.Inject(Markets.Test);
        this.Inject(Users.Test);
    }

    [Fact]
    public void Signature_IsValid()
    {
        // arrange
        var service = Get<SignatureService>();
        var expectedSignature = "0faa87b885c8901ac1d20c21d9181a404f5a2db737f5338f3e4384d0f0069b6e";
        var query =
            "symbol=LTCBTC&side=BUY&type=LIMIT&timeInForce=GTC&quantity=1&price=0.1&recvWindow=5000&timestamp=1499827319559";

        // act
        var signature = service.GetSignature(query);

        // assert
        service.GetKey().Is(Users.Test.Key);
        signature.Is(expectedSignature);
    }
}
