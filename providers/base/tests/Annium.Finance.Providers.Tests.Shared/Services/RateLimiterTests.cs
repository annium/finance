using System.Threading.Tasks;
using Annium.Finance.Providers.Shared.Services;
using Annium.Logging;
using Annium.Testing;
using Xunit;

namespace Annium.Finance.Providers.Tests.Shared.Services;

public class RateLimiterTests : ProvidersTestBase
{
    public RateLimiterTests(ITestOutputHelper outputHelper)
        : base(outputHelper) { }

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

    private IRateLimiter CreateLimiter() => Get<IRateLimiterFactory>().CreateRateLimiter(100, 10, 10);
}
