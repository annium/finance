using Annium.Finance.Providers.Abstractions.Domain.Market;
using Annium.Finance.Providers.Abstractions.Domain.Shared;
using Annium.Finance.Providers.Abstractions.Domain.User;
using Annium.Finance.Providers.Tests.Lib;

namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Tests;

/// <summary>
/// Shared settings for the tests that talk to the real Binance USD-M futures exchange: the market/environment
/// key to connect against, and the API credentials and expected request signature read from <c>test.env</c>.
/// </summary>
internal static class Settings
{
    /// <summary>The market settings (provider and environment) used to resolve the live market connector/provider.</summary>
    public static readonly MarketSettings Market = new()
    {
        Provider = Constants.Provider,
        Environment = ProviderEnvironment.Real,
    };

    /// <summary>
    /// Gets the user settings, including the API key/secret read from <c>test.env</c>, used to resolve the
    /// live user provider. Read on first use rather than in a field initializer: a static field is run by
    /// the type initializer, so touching <see cref="Market"/> alone - which the market tests do, and which
    /// needs no credentials at all - would go looking for them and fail where none are configured.
    /// </summary>
    public static UserSettings User =>
        field ??= new UserSettings
        {
            Provider = Constants.Provider,
            Environment = ProviderEnvironment.Real,
            Key = TestEnv.GetVariable("TEST_KEY"),
            Secret = TestEnv.GetVariable("TEST_SECRET"),
        };

    /// <summary>
    /// Gets the expected HMAC signature for the fixed request used by <c>SignatureServiceTests</c>, read from
    /// <c>test.env</c> on first use, for the same reason as <see cref="User"/>.
    /// </summary>
    public static string ExpectedSignature => field ??= TestEnv.GetVariable("TEST_EXPECTED_SIGNATURE");
}
