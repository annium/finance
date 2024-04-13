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
        position.AddLimitBuyOrder(2, 1).Data.IsActive().IsTrue();
        position.AddLimitBuyOrder(2, 1).FillPartially(1).Data.IsActive().IsTrue();
        position.AddLimitBuyOrder(2, 1).Fill().Data.IsActive().IsFalse();
        position.AddLimitBuyOrder(2, 1).Cancel().Data.IsActive().IsFalse();
    }

    [Fact]
    public void IsInactive()
    {
        // arrange
        var position = Helper.CreatePosition(1);

        // assert
        position.AddLimitBuyOrder(2, 1).Data.IsInactive().IsFalse();
        position.AddLimitBuyOrder(2, 1).FillPartially(1).Data.IsInactive().IsFalse();
        position.AddLimitBuyOrder(2, 1).Fill().Data.IsInactive().IsTrue();
        position.AddLimitBuyOrder(2, 1).Cancel().Data.IsInactive().IsTrue();
    }

    [Fact]
    public void IsImmediate()
    {
        // arrange
        var position = Helper.CreatePosition(1);

        // assert
        position.AddLimitBuyOrder(2, 1).Fill().Data.IsImmediate().IsTrue();
        position.AddStopLossMarketSellOrder(1, 0.5m).Data.IsImmediate().IsFalse();
        position.AddTakeProfitMarketSellOrder(1, 1.5m).Data.IsImmediate().IsFalse();
    }

    [Fact]
    public void IsLeveled()
    {
        // arrange
        var position = Helper.CreatePosition(1);

        // assert
        position.AddLimitBuyOrder(2, 1).Fill().Data.IsLimit().IsTrue();
        position.AddStopLossMarketSellOrder(1, 0.5m).Data.IsLimit().IsFalse();
        position.AddTakeProfitMarketSellOrder(1, 1.5m).Data.IsLimit().IsFalse();
    }

    [Fact]
    public void IsLimit()
    {
        // arrange
        var position = Helper.CreatePosition(1);

        // assert
        position.AddLimitBuyOrder(2, 1).Fill().Data.IsLimit().IsTrue();
        position.AddStopLossMarketSellOrder(1, 0.5m).Data.IsLimit().IsFalse();
        position.AddTakeProfitMarketSellOrder(1, 1.5m).Data.IsLimit().IsFalse();
    }

    [Fact]
    public void IsMarket()
    {
        // arrange
        var position = Helper.CreatePosition(1);

        // assert
        position.AddLimitBuyOrder(2, 1).Fill().Data.IsMarket().IsFalse();
        position.AddStopLossMarketSellOrder(1, 0.5m).Data.IsMarket().IsTrue();
        position.AddTakeProfitMarketSellOrder(1, 1.5m).Data.IsMarket().IsTrue();
    }

    [Fact]
    public void OpeningQty()
    {
        // arrange
        var position = Helper.CreatePosition(1);

        // assert
        position.AddLimitBuyOrder(2, 1).Data.OpeningQty().Is(2);
        position.AddLimitBuyOrder(2, 1).FillPartially(1).Data.OpeningQty().Is(1);
        position.AddLimitBuyOrder(2, 1).Fill().Data.OpeningQty().Is(0);
        position.AddLimitBuyOrder(2, 1).FillPartially(0.5m).Cancel().Data.OpeningQty().Is(0);
    }

    [Fact]
    public void PotentialQty()
    {
        // arrange
        var position = Helper.CreatePosition(1);

        // assert
        position.AddLimitBuyOrder(2, 1).Data.PotentialQty().Is(2);
        position.AddLimitBuyOrder(2, 1).FillPartially(1).Data.PotentialQty().Is(2);
        position.AddLimitBuyOrder(2, 1).Fill().Data.PotentialQty().Is(2);
        position.AddLimitBuyOrder(2, 1).FillPartially(0.5m).Cancel().Data.PotentialQty().Is(0.5m);
    }

    [Fact]
    public void CanceledQty()
    {
        // arrange
        var position = Helper.CreatePosition(1);

        // assert
        position.AddLimitBuyOrder(2, 1).Data.CancellableQty().Is(0);
        position.AddLimitBuyOrder(2, 1).FillPartially(1).Data.CancellableQty().Is(0);
        position.AddLimitBuyOrder(2, 1).Fill().Data.CancellableQty().Is(0);
        position.AddLimitBuyOrder(2, 1).FillPartially(0.5m).Cancel().Data.CancellableQty().Is(1.5m);
    }
}
