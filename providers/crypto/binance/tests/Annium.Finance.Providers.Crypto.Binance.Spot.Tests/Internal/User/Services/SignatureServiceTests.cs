using Annium.Finance.Providers.Abstractions.Domain.Shared;
using Annium.Finance.Providers.Core;
using Annium.Finance.Providers.Crypto.Binance.Base.User;
using Annium.Finance.Providers.Tests.Lib;
using Annium.Testing;
using Xunit;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Tests.Internal.User.Services;

public class SignatureServiceTests : ProvidersTestBase
{
    public SignatureServiceTests(ITestOutputHelper outputHelper)
        : base(outputHelper) { }

    protected override void RegisterProvider(ProviderRegistrationContext ctx)
    {
        ctx.WithBinanceSpot();
    }

    [Fact]
    public void Signature_IsValid()
    {
        // arrange
        var settings = Settings.User;
        var providerKey = settings.GetProviderKey();
        var service = Provider.CreateSignatureService(settings, providerKey);
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
