using Annium.Testing;
using Xunit;
using static Annium.Finance.Providers.Abstractions.Domain.User.Helpers.PositionHelper;
using static Annium.Finance.Providers.Abstractions.Domain.User.PositionState;

namespace Annium.Finance.Providers.Abstractions.Domain.Tests.User.Helpers;

public class PositionHelperTests
{
    [Fact]
    public void ResolveState_InvalidState_Fails()
    {
        var subject = "demo";

        // assert
        // opening + opened is greater than total
        ResolveState(subject, 10, 6, 5, 0, 0).PlainErrors.At(0).IsContaining("too much opens");
        // closing + closed is greater than opening + opened
        ResolveState(subject, 10, 0, 10, 6, 5).PlainErrors.At(0).IsContaining("too much closes");
    }

    [Fact]
    public void ResolveState_Blank()
    {
        var subject = "demo";

        // assert
        ResolveState(subject, 0, 0, 0, 0, 0).Data.Is(Blank);
    }

    [Fact]
    public void ResolveState_Active()
    {
        var subject = "demo";

        // assert
        ResolveState(subject, 2, 1, 1, 1, 1).Data.Is(Opening | Opened | Closing | Closed);
    }

    [Fact]
    public void ResolveState_Filled()
    {
        var subject = "demo";

        // assert
        ResolveState(subject, 2, 0, 1, 0, 1).Data.Is(Filled);
    }

    [Fact]
    public void ResolveState_Canceled()
    {
        var subject = "demo";

        // assert
        ResolveState(subject, 2, 0, 0, 0, 0).Data.Is(Canceled);
    }

    [Fact]
    public void ResolvePrice_Ok()
    {
        var subject = "demo";

        // assert
        ResolvePrice(subject, 0, 0, 0, 0).Data.Is(0);
        ResolvePrice(subject, 0, 0, 5, 20).Data.Is(20);
        ResolvePrice(subject, 5, 10, 15, 20).Data.Is(17.5m);
        ResolvePrice(subject, -5, 10, 15, 20).PlainErrors.Has(1);
    }
}
