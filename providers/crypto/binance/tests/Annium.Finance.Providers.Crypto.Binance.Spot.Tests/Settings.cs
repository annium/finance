using Annium.Finance.Providers.Abstractions.Domain.Market;
using Annium.Finance.Providers.Abstractions.Domain.Shared;
using Annium.Finance.Providers.Abstractions.Domain.User;
using Annium.Finance.Providers.Tests.Lib;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Tests;

/// <summary>
/// Shared settings for the tests that talk to the real Binance Spot exchange: the market/environment key to
/// connect against, and the API credentials and expected request signature read from <c>test.env</c>.
/// </summary>
internal static class Settings
{
    /// <summary>The market settings (provider and environment) used to resolve the live market connector/provider.</summary>
    public static readonly MarketSettings Market = new()
    {
        Provider = Constants.Provider,
        Environment = ProviderEnvironment.Real,
    };

    /// <summary>The user settings, including the API key/secret read from <c>test.env</c>, used to resolve the live user provider.</summary>
    public static readonly UserSettings User = new()
    {
        Provider = Constants.Provider,
        Environment = ProviderEnvironment.Real,
        Key = TestEnv.GetVariable("TEST_KEY"),
        Secret = TestEnv.GetVariable("TEST_SECRET"),
    };

    /// <summary>The expected HMAC signature for the fixed request used by <c>SignatureServiceTests</c>, read from <c>test.env</c>.</summary>
    public static readonly string ExpectedSignature = TestEnv.GetVariable("TEST_EXPECTED_SIGNATURE");
}
