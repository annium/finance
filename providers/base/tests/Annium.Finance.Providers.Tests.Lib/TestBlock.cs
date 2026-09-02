namespace Annium.Finance.Providers.Tests.Lib;

/// <summary>
/// The trait every test class is sorted by, so a run can ask for the cheap tests, the ones that read from a
/// real exchange, or the ones that trade on it.
/// </summary>
/// <remarks>
/// This is the only thing separating a routine run from one that trades, which makes marking a test part
/// of writing it rather than a later tidy-up. There used to be a second mechanism - an environment
/// variable each exchange test was gated on - and it was dropped deliberately: it protected against a
/// trait being wrong, and a trait being wrong is an accepted risk here, while the cost was that every
/// live run needed a variable set from somewhere the recipe could not see.
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
