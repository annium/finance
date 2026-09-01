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
    /// Gets a value indicating whether exchange credentials are available. Some tests need a key and a
    /// secret without placing an order - request signing, for one - and those run wherever the credentials
    /// are, rather than being tied to the switch that permits trading.
    /// </summary>
    /// <remarks>
    /// This gate says nothing about the network. The signature tests resolve their service through the
    /// container, and resolving one pulls in a keyed <c>IServerTimeSource</c>, which begins polling the
    /// exchange's public server-time endpoint from its constructor. Nothing signed or placed - but a test
    /// behind this gate and not behind <see cref="IsEnabled"/> does reach the exchange. Starting network
    /// work from a constructor is what makes it unavoidable here; fixing that belongs with the exchange
    /// areas rather than this gate.
    /// </remarks>
    public static bool HasCredentials => TestEnv.IsAvailable;
}
