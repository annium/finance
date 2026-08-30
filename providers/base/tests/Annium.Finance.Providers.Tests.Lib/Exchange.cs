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
}
