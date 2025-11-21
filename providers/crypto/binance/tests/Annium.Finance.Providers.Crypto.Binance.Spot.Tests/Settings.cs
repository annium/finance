using Annium.Finance.Providers.Abstractions.Domain.Enums;
using Annium.Finance.Providers.Abstractions.Domain.Models;
using Annium.Finance.Providers.Tests.Lib;

namespace Annium.Finance.Providers.Crypto.Binance.Spot.Tests;

internal static class Settings
{
    public static readonly MarketSettings Market = new()
    {
        Provider = Constants.Provider,
        Environment = ProviderEnvironment.Real,
    };

    public static readonly UserSettings User = new()
    {
        Provider = Constants.Provider,
        Environment = ProviderEnvironment.Real,
        Key = TestEnv.GetVariable("TEST_KEY"),
        Secret = TestEnv.GetVariable("TEST_SECRET"),
    };

    public static readonly string ExpectedSignature = TestEnv.GetVariable("TEST_EXPECTED_SIGNATURE");
}
