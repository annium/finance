using Annium.Testing;
using Xunit;
using static Annium.Finance.Providers.Abstractions.Domain.User.Helpers.PositionHelper;
using static Annium.Finance.Providers.Abstractions.Domain.User.PositionState;

namespace Annium.Finance.Providers.Abstractions.Domain.Tests.User.Helpers;

/// <summary>
/// Pins the state-resolution and price-averaging rules in <see cref="Annium.Finance.Providers.Abstractions.Domain.User.Helpers.PositionHelper"/>:
/// which combinations of opening/opened/closing/closed quantities are internally inconsistent, which
/// <see cref="Annium.Finance.Providers.Abstractions.Domain.User.PositionState"/> each consistent combination
/// resolves to, and how a new fill blends into the running average price.
/// </summary>
public class PositionHelperTests
{
    /// <summary>
    /// Verifies that <see cref="Annium.Finance.Providers.Abstractions.Domain.User.Helpers.PositionHelper.ResolveState{T}"/>
    /// reports an error, rather than a state, when opens exceed the total quantity or closes exceed the opened
    /// quantity.
    /// </summary>
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

    /// <summary>Verifies that a position with a zero total quantity resolves to <see cref="Annium.Finance.Providers.Abstractions.Domain.User.PositionState.Blank"/>.</summary>
    [Fact]
    public void ResolveState_Blank()
    {
        var subject = "demo";

        // assert
        ResolveState(subject, 0, 0, 0, 0, 0).Data.Is(Blank);
    }

    /// <summary>Verifies that a position with quantity in every one of opening, opened, closing and closed resolves to the combination of all four flags.</summary>
    [Fact]
    public void ResolveState_Active()
    {
        var subject = "demo";

        // assert
        ResolveState(subject, 2, 1, 1, 1, 1).Data.Is(Opening | Opened | Closing | Closed);
    }

    /// <summary>Verifies that a position whose opened quantity has been fully closed resolves to <see cref="Annium.Finance.Providers.Abstractions.Domain.User.PositionState.Filled"/> rather than the raw Opened|Closed combination.</summary>
    [Fact]
    public void ResolveState_Filled()
    {
        var subject = "demo";

        // assert
        ResolveState(subject, 2, 0, 1, 0, 1).Data.Is(Filled);
    }

    /// <summary>
    /// Verifies that a position which has closed only part of what it opened stays reported as opened and
    /// closing, not as filled. Filled is the state that says a position is done with, so reporting it while
    /// quantity is still held would tell a caller an exposure had been closed that is in fact still open.
    /// </summary>
    [Fact]
    public void ResolveState_PartiallyClosed_IsNotFilled()
    {
        var subject = "demo";

        // assert - two opened, one closed: the same flags a fully closed position carries, and only the
        // quantities tell the two apart
        ResolveState(subject, 2, 0, 2, 0, 1).Data.Is(Opened | Closed);
        ResolveState(subject, 2, 0, 2, 0, 2).Data.Is(Filled, "closing all of what was opened is filled");
    }

    /// <summary>Verifies that a position with a positive total but no quantity in any of the four buckets resolves to <see cref="Annium.Finance.Providers.Abstractions.Domain.User.PositionState.Canceled"/>.</summary>
    [Fact]
    public void ResolveState_Canceled()
    {
        var subject = "demo";

        // assert
        ResolveState(subject, 2, 0, 0, 0, 0).Data.Is(Canceled);
    }

    /// <summary>
    /// Verifies that <see cref="Annium.Finance.Providers.Abstractions.Domain.User.Helpers.PositionHelper.ResolvePrice{T}"/>
    /// volume-weights the current and newly executed price by their quantities, returns zero when there is no
    /// quantity at all, and reports an error for a negative quantity.
    /// </summary>
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
