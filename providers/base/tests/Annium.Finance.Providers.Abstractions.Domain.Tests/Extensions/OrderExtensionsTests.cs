using Annium.Finance.Providers.Abstractions.Domain.Extensions;
using Annium.Finance.Providers.Tests.Lib;
using Annium.Testing;
using Xunit;

namespace Annium.Finance.Providers.Abstractions.Domain.Tests.Extensions;

public class OrderExtensionsTests
{
    [Fact]
    public void IsActive()
    {
        // arrange
        var position = Helper.CreatePosition(1);

        // assert
        position.AddLimitBuyOrder(2, 1).IsActive().IsTrue();
        position.AddLimitBuyOrder(2, 1).FillPartially(1).IsActive().IsTrue();
        position.AddLimitBuyOrder(2, 1).Fill().IsActive().IsFalse();
        position.AddLimitBuyOrder(2, 1).Cancel().IsActive().IsFalse();
    }

    [Fact]
    public void IsInactive()
    {
        // arrange
        var position = Helper.CreatePosition(1);

        // assert
        position.AddLimitBuyOrder(2, 1).IsInactive().IsFalse();
        position.AddLimitBuyOrder(2, 1).FillPartially(1).IsInactive().IsFalse();
        position.AddLimitBuyOrder(2, 1).Fill().IsInactive().IsTrue();
        position.AddLimitBuyOrder(2, 1).Cancel().IsInactive().IsTrue();
    }

    [Fact]
    public void IsImmediate()
    {
        // arrange
        var position = Helper.CreatePosition(1);

        // assert
        position.AddLimitBuyOrder(2, 1).Fill().IsImmediate().IsTrue();
        position.AddStopLossMarketSellOrder(1, 0.5m).IsImmediate().IsFalse();
        position.AddTakeProfitMarketSellOrder(1, 1.5m).IsImmediate().IsFalse();
    }

    [Fact]
    public void IsLeveled()
    {
        // arrange
        var position = Helper.CreatePosition(1);

        // assert
        position.AddLimitBuyOrder(2, 1).Fill().IsLimit().IsTrue();
        position.AddStopLossMarketSellOrder(1, 0.5m).IsLimit().IsFalse();
        position.AddTakeProfitMarketSellOrder(1, 1.5m).IsLimit().IsFalse();
    }

    [Fact]
    public void IsLimit()
    {
        // arrange
        var position = Helper.CreatePosition(1);

        // assert
        position.AddLimitBuyOrder(2, 1).Fill().IsLimit().IsTrue();
        position.AddStopLossMarketSellOrder(1, 0.5m).IsLimit().IsFalse();
        position.AddTakeProfitMarketSellOrder(1, 1.5m).IsLimit().IsFalse();
    }

    [Fact]
    public void IsMarket()
    {
        // arrange
        var position = Helper.CreatePosition(1);

        // assert
        position.AddLimitBuyOrder(2, 1).Fill().IsMarket().IsFalse();
        position.AddStopLossMarketSellOrder(1, 0.5m).IsMarket().IsTrue();
        position.AddTakeProfitMarketSellOrder(1, 1.5m).IsMarket().IsTrue();
    }

    [Fact]
    public void PotentialQty()
    {
        // arrange
        var position = Helper.CreatePosition(1);

        // assert
        position.AddLimitBuyOrder(2, 1).PotentialQty().Is(2);
        position.AddLimitBuyOrder(2, 1).FillPartially(1).PotentialQty().Is(2);
        position.AddLimitBuyOrder(2, 1).Fill().PotentialQty().Is(2);
        position.AddLimitBuyOrder(2, 1).FillPartially(0.5m).Cancel().PotentialQty().Is(0.5m);
    }

    [Fact]
    public void CanceledQty()
    {
        // arrange
        var position = Helper.CreatePosition(1);

        // assert
        position.AddLimitBuyOrder(2, 1).CancellableQty().Is(0);
        position.AddLimitBuyOrder(2, 1).FillPartially(1).CancellableQty().Is(0);
        position.AddLimitBuyOrder(2, 1).Fill().CancellableQty().Is(0);
        position.AddLimitBuyOrder(2, 1).FillPartially(0.5m).Cancel().CancellableQty().Is(1.5m);
    }
}
