using Annium.Finance.Providers.Abstractions.Domain.Shared;
using Annium.Finance.Providers.Abstractions.Domain.User;
using Annium.Testing;
using Xunit;

namespace Annium.Finance.Providers.Abstractions.Domain.Tests.User;

/// <summary>
/// Pins how <see cref="UserSettings.ToString"/> identifies an account without exposing its credentials.
/// </summary>
public class UserSettingsTests
{
    /// <summary>
    /// Verifies that the description carries the provider, the environment and only a prefix of the key,
    /// never the key in full and never the secret.
    /// </summary>
    [Fact]
    public void ToString_ShowsOnlyAKeyPrefix()
    {
        // arrange
        var settings = new UserSettings
        {
            Provider = "binance",
            Environment = ProviderEnvironment.Test,
            Key = "0123456789abcdef",
            Secret = "s3cr3t",
        };

        // assert
        settings.ToString().Is("binance[Test]0123456");
    }

    /// <summary>
    /// Verifies that a key shorter than the prefix - an unset one included - is described rather than
    /// rejected. The description is built from a connector's constructor, so throwing here aborts
    /// construction with a range error that names neither the setting at fault nor its account.
    /// </summary>
    /// <param name="key">The key to describe.</param>
    /// <param name="expected">The description the settings are expected to produce.</param>
    [Theory]
    [InlineData("", "binance[Test]")]
    [InlineData("abc", "binance[Test]abc")]
    [InlineData("0123456", "binance[Test]0123456")]
    public void ToString_ShortKey_IsDescribedNotRejected(string key, string expected)
    {
        // arrange
        var settings = new UserSettings
        {
            Provider = "binance",
            Environment = ProviderEnvironment.Test,
            Key = key,
        };

        // assert
        settings.ToString().Is(expected);
    }
}
