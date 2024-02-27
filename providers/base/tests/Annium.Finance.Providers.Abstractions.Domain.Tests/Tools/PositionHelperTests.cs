using System;
using Annium.Finance.Providers.Abstractions.Domain.Enums;
using Annium.Testing;
using Xunit;
using static Annium.Finance.Providers.Abstractions.Domain.Tools.PositionHelper;

namespace Annium.Finance.Providers.Abstractions.Domain.Tests.Tools;

public class PositionHelperTests
{
    [Fact]
    public void ResolveState_InvalidState_Fails()
    {
        // assert
        // opening + opened is greater than total
        Wrap.It(() => ResolveState(1, 0.6m, 0.5m, 0, 0)).Throws<InvalidOperationException>();
        // closing + closed is greater than opening + opened
        Wrap.It(() => ResolveState(1, 0, 1, 0.6m, 0.5m)).Throws<InvalidOperationException>();
        // closing is greater than opened
        Wrap.It(() => ResolveState(1, 0, 1, 0.6m, 0.5m)).Throws<InvalidOperationException>();
    }

    [Fact]
    public void ResolveState_Blank()
    {
        // assert
        ResolveState(0, 0, 0, 0, 0).Is(PositionState.Blank);
    }

    [Fact]
    public void ResolveState_Opening_Closing()
    {
        // assert
        ResolveState(2, 1, 1, 1, 1).Is(PositionState.Opening | PositionState.Closing);
    }

    [Fact]
    public void ResolveState_Opening()
    {
        // assert
        ResolveState(2, 1, 1, 0, 0).Is(PositionState.Opening);
    }

    [Fact]
    public void ResolveState_Closing()
    {
        // assert
        ResolveState(2, 0, 2, 1, 1).Is(PositionState.Closing);
    }

    [Fact]
    public void ResolveState_Opened()
    {
        // assert
        ResolveState(2, 0, 1, 0, 0).Is(PositionState.Opened);
        ResolveState(2, 0, 2, 0, 1).Is(PositionState.Opened);
    }

    [Fact]
    public void ResolveState_Closed()
    {
        // assert
        ResolveState(2, 0, 1, 0, 1).Is(PositionState.Closed);
        ResolveState(2, 0, 2, 0, 2).Is(PositionState.Closed);
    }

    [Fact]
    public void ResolveState_Canceled()
    {
        // assert
        ResolveState(2, 0, 0, 0, 0).Is(PositionState.Canceled);
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
