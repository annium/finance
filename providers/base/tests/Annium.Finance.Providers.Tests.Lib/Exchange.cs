using System;

namespace Annium.Finance.Providers.Tests.Lib;

/// <summary>
/// Reports whether the credentials the exchange-facing tests need are present.
/// </summary>
/// <remarks>
/// This is a condition of possibility, not a permission. What decides whether an exchange test is asked
/// for at all is the block it carries - see <see cref="TestBlock"/> - and <c>just test</c> selects
/// neither the read block nor the write one. Without credentials the tests that need them cannot run,
/// so they say so rather than failing on a missing key.
///
/// It says nothing about the network: a test behind this gate may still reach the exchange, and the two
/// signature tests do, through a server time source that begins polling from its constructor.
/// </remarks>
public static class Exchange
{
    /// <summary>
    /// Gets a value indicating whether exchange credentials are available in <c>test.env</c>.
    /// </summary>
    public static bool HasCredentials => TestEnv.IsAvailable;
}
