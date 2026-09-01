namespace Annium.Finance.Providers.Tests.Lib;

/// <summary>
/// The trait every test class is sorted by, so a run can ask for the cheap tests, the ones that read from a
/// real exchange, or the ones that trade on it.
/// </summary>
/// <remarks>
/// This decides **selection**, never safety. What keeps a trading test from running is its
/// <c>SkipUnless</c> gate on <see cref="Exchange.IsEnabled"/>, and that gate holds however the class is
/// marked - so a mislabelled test runs in the wrong block at worst, and cannot reach the exchange by
/// accident. The two mechanisms are deliberately independent; collapsing them into one would make a typo in
/// a trait name enough to place an order.
///
/// Absence of the trait means <c>offline</c>. That is the safe default in the direction that matters: a new
/// test nobody marked joins the block that is always run, rather than the block that is never run.
/// </remarks>
public static class TestBlock
{
    /// <summary>The trait name every block is expressed with.</summary>
    public const string Name = "block";

    /// <summary>Connects to a real exchange and a real account, and mutates nothing.</summary>
    public const string Read = "read";

    /// <summary>Mutates the account: places orders, opens and closes positions.</summary>
    public const string Write = "write";
}
