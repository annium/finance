using Annium.Core.Mapper.Attributes;

namespace Annium.Finance.Providers.Abstractions.Domain.User;

[AutoMapped]
public enum OrderType
{
    Limit,
    Market,
    StopLossLimit,
    StopLossMarket,
    TakeProfitLimit,
    TakeProfitMarket,
}
