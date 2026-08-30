using System;

namespace Annium.Finance.Providers.Tests.Lib;

/// <summary>
/// Gate for the tests that talk to a real exchange. They need credentials in test.env, and the order ones
/// place and cancel actual orders, so a routine run must not reach them - they are skipped unless asked
/// for explicitly.
/// </summary>
public static class Exchange
{
    /// <summary>
    /// Gets a value indicating whether tests against the live exchange should run.
    /// </summary>
    public static bool IsEnabled => Environment.GetEnvironmentVariable("FINANCE_EXCHANGE_TESTS") == "1";

    /// <summary>
    /// Gets a value indicating whether exchange credentials are available. Some tests need a key and
    /// secret without going near the network - request signing, for one - and those run wherever the
    /// credentials are, rather than being tied to the switch that permits trading.
    /// </summary>
    public static bool HasCredentials => TestEnv.IsAvailable;
}
