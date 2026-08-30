using Annium.Finance.Providers.Abstractions.Domain.Shared;
using Annium.Finance.Providers.Core;
using Annium.Finance.Providers.Crypto.Binance.Base.User;
using Annium.Finance.Providers.Tests.Lib;
using Annium.Testing;
using Xunit;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Tests.Internal.User.Services;

/// <summary>
/// Verifies that the signature service signs a request query with HMAC-SHA256 over the API secret exactly
/// the way Binance expects, by checking a fixed query string against a signature pinned in <c>test.env</c>
/// rather than against a live account.
/// </summary>
public class SignatureServiceTests : ProvidersTestBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SignatureServiceTests"/> class.
    /// </summary>
    /// <param name="outputHelper">The xUnit output helper to route trace logging to.</param>
    public SignatureServiceTests(ITestOutputHelper outputHelper)
        : base(outputHelper) { }

    /// <summary>
    /// Registers the Binance Spot provider so the signature service under test is resolved from its actual registration.
    /// </summary>
    /// <param name="ctx">The fluent context to register providers into.</param>
    protected override void RegisterProvider(ProviderRegistrationContext ctx)
    {
        ctx.WithBinanceSpot();
    }

    /// <summary>
    /// Signs a fixed order query string with the credentials from <c>test.env</c> and asserts it matches the
    /// signature pinned in <see cref="Settings.ExpectedSignature"/>.
    /// </summary>
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
