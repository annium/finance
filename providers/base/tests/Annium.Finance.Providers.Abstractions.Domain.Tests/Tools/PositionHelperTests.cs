using System;
using Annium.Testing;
using Xunit;
using static Annium.Finance.Providers.Abstractions.Domain.Enums.PositionState;
using static Annium.Finance.Providers.Abstractions.Domain.Tools.PositionHelper;

namespace Annium.Finance.Providers.Abstractions.Domain.Tests.Tools;

public class PositionHelperTests
{
    [Fact]
    public void ResolveState_InvalidState_Fails()
    {
        // assert
        // opening + opened is greater than total
        Wrap.It(() => ResolveState(10, 6, 5, 0, 0)).Throws<InvalidOperationException>();
        // closing + closed is greater than opening + opened
        Wrap.It(() => ResolveState(10, 0, 10, 6, 5)).Throws<InvalidOperationException>();
    }

    [Fact]
    public void ResolveState_Blank()
    {
        // assert
        ResolveState(0, 0, 0, 0, 0).Is(Blank);
    }

    [Fact]
    public void ResolveState_Active()
    {
        // assert
        ResolveState(2, 1, 1, 1, 1).Is(Opening | Opened | Closing | Closed);
    }

    [Fact]
    public void ResolveState_Filled()
    {
        // assert
        ResolveState(2, 0, 1, 0, 1).Is(Filled);
    }

    [Fact]
    public void ResolveState_Canceled()
    {
        // assert
        ResolveState(2, 0, 0, 0, 0).Is(Canceled);
    }

    [Fact]
    public void ResolvePrice_Ok()
    {
        // assert
        ResolvePrice(0, 0, 0, 0).Is(0);
        ResolvePrice(0, 0, 5, 20).Is(20);
        ResolvePrice(5, 10, 15, 20).Is(17.5m);
    }
}
