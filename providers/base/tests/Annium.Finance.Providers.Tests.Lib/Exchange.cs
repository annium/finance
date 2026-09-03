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
///
/// One type serves every provider, and that works because each test assembly runs in its own process:
/// the answer is about the credentials reachable from that process, whether they come from the project's
/// own <c>test.env</c> or from its scoped environment variables.
/// </remarks>
public static class Exchange
{
    /// <summary>
    /// Gets a value indicating whether exchange credentials are available to this test assembly.
    /// </summary>
    /// <remarks>
    /// Names the two variables every provider's user settings are built from, rather than asking whether
    /// any variable at all was configured. The old count-based answer was true for a file holding one
    /// unrelated entry, which let the gated tests run and fail deep inside a signed request; and a count
    /// cannot span the environment at all, where there is no set of names to count.
    ///
    /// <c>TEST_EXPECTED_SIGNATURE</c> is deliberately not part of the gate: only the two signature tests
    /// need it, and a missing one should name itself through <see cref="TestEnv.GetVariable"/> rather than
    /// silently skip every user test in the assembly.
    /// </remarks>
    public static bool HasCredentials => TestEnv.Has("TEST_KEY") && TestEnv.Has("TEST_SECRET");
}
