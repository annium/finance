using System.Threading.Tasks;
using Annium.Finance.Providers.Core.Shared;
using Annium.Finance.Providers.Core.Shared.RateLimits;
using Annium.Finance.Providers.Tests.Lib;
using Annium.Logging;
using Annium.Testing;
using Xunit;

namespace Annium.Finance.Providers.Core.Tests.Shared.RateLimits;

/// <summary>
/// Pins the water-mark gating behavior of <see cref="IRateLimiter"/>: that <see cref="IRateLimiter.CanExecute"/>
/// tracks the reported used weight relative to the configured limit, and that weight reported above the water
/// mark decays back down over time on its own.
/// </summary>
public class RateLimiterTests : ProvidersTestBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RateLimiterTests"/> class.
    /// </summary>
    /// <param name="outputHelper">The xUnit output helper used to capture test logs.</param>
    public RateLimiterTests(ITestOutputHelper outputHelper)
        : base(outputHelper) { }

    /// <summary>
    /// Verifies that <see cref="IRateLimiter.CanExecute"/> allows requests below the water mark and denies them at
    /// or above it, tracking each new value reported via <see cref="IRateLimiter.UsedWeight"/>.
    /// </summary>
    [Fact]
    public void CanExecute_WaterMarkLevels_Ok()
    {
        using var limiter = CreateLimiter();

        this.Trace("at 0");
        limiter.CanExecute().IsTrue();

        this.Trace("below watermark");
        limiter.UsedWeight(50);
        limiter.CanExecute().IsTrue();

        this.Trace("at watermark");
        limiter.UsedWeight(80);
        limiter.CanExecute().IsFalse();

        this.Trace("above watermark");
        limiter.UsedWeight(100);
        limiter.CanExecute().IsFalse();

        this.Trace("below watermark");
        limiter.UsedWeight(50);
        limiter.CanExecute().IsTrue();
    }

    /// <summary>
    /// A limit narrowed after construction takes effect. An exchange reports its rate limits in the
    /// responses it sends, so the limit a provider starts with is a guess it is expected to correct - and
    /// a correction that changes nothing leaves the limiter permitting requests the exchange will refuse.
    /// </summary>
    [Fact]
    public void UpdateLimit_AfterConstruction_MovesTheWaterMark()
    {
        using var limiter = CreateLimiter();

        // a weight the original limit allows
        limiter.UsedWeight(50);
        limiter.CanExecute().IsTrue();

        // the same weight, against a limit half as generous
        limiter.UpdateLimit(50);
        limiter.CanExecute().IsFalse("a narrowed limit must refuse a weight the wider one allowed");

        // and widening again lets it through
        limiter.UpdateLimit(100);
        limiter.CanExecute().IsTrue("a widened limit must allow what it covers");
    }

    /// <summary>
    /// Verifies that used weight reported above the water mark eventually decays back below it on its own,
    /// without any further calls, letting <see cref="IRateLimiter.CanExecute"/> allow requests again.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task UsedWeight_AboveWaterMark_LowersPeriodicallyUntilBelow()
    {
        using var limiter = CreateLimiter();

        this.Trace("go above watermark");
        limiter.UsedWeight(190);
        limiter.CanExecute().IsFalse();

        this.Trace("wait to lower below watermark");
        await Expect.ToAsync(() => limiter.CanExecute().IsTrue());

        this.Trace("done");
    }

    /// <summary>Creates a rate limiter with a limit of 100 (80 water mark), decaying used weight by 10 every 10ms once above it.</summary>
    /// <returns>The constructed rate limiter.</returns>
    private IRateLimiter CreateLimiter() => Provider.CreateRateLimiter(100, 10, 10);
}
