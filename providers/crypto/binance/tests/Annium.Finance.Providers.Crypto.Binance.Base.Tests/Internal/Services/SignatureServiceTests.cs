using Annium.Finance.Providers.Crypto.Binance.Base.Services;
using Xunit;

namespace Annium.Finance.Providers.Crypto.Binance.Base.Tests.Internal.Services;

public class SignatureServiceTests
{
    [Fact]
    public void Signature_IsValid()
    {
        // arrange
        const string apiKey = "vmPUZE6mv9SD5VNHk4HlWFsOr6aKE2zvsw0MuIgwCIPy6utIco14y7Ju91duEh8A";
        const string apiSecret = "NhqPtmdSJYdKjVHjA7PZj4Mge3R5YNiP1e3UZjInClVN65XAbvqqM6A7H5fATj0j";
        var service = new SignatureService(apiKey, apiSecret);
        var expectedSignature = "c8db56825ae71d6d79447849e617115f4a920fa2acdcab2b053c4b2838bd6b71";
        var query =
            "symbol=LTCBTC&side=BUY&type=LIMIT&timeInForce=GTC&quantity=1&price=0.1&recvWindow=5000&timestamp=1499827319559";

        // act
        var signature = service.GetSignature(query).ToString();

        // assert
        Assert.Equal(apiKey, service.GetKey().ToString());
        Assert.Equal(expectedSignature, signature);
    }
}
