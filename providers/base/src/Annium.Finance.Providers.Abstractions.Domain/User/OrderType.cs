using Annium.Core.Mapper.Attributes;

namespace Annium.Finance.Providers.Abstractions.Domain.User;

/// <summary>
/// Identifies how and at what price an order executes.
/// </summary>
[AutoMapped]
public enum OrderType
{
    /// <summary>Executes at a specified price or better.</summary>
    Limit,

    /// <summary>Executes immediately at the current market price.</summary>
    Market,

    /// <summary>Becomes a limit order once the market reaches a specified trigger (level) price, used to cap a loss.</summary>
    StopLossLimit,

    /// <summary>Becomes a market order once the market reaches a specified trigger (level) price, used to cap a loss.</summary>
    StopLossMarket,

    /// <summary>Becomes a limit order once the market reaches a specified trigger (level) price, used to lock in a gain.</summary>
    TakeProfitLimit,

    /// <summary>Becomes a market order once the market reaches a specified trigger (level) price, used to lock in a gain.</summary>
    TakeProfitMarket,
}
