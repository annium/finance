using Annium.Finance.Providers.Abstractions.Domain.Enums;
using Annium.Finance.Providers.Abstractions.Domain.Interfaces;

namespace Annium.Finance.Providers.Abstractions.Domain.Dto;

public sealed record OrderDto : IOrder
{
    public string Id { get; }
    public string ClientOrderId { get; }
    public string Symbol { get; }
    public OrderSide Side { get; private set; }
    public OrderType Type { get; private set; }
    public decimal TotalQty { get; private set; }
    public decimal Price { get; private set; }
    public decimal LevelPrice { get; private set; }
    public long CreatedAt { get; private set; }
    public OrderStatus Status { get; private set; }
    public decimal ExecutedQty { get; private set; }
    public decimal ExecutedPrice { get; private set; }
    public decimal Fee { get; private set; }
    public long UpdatedAt { get; private set; }

    public OrderDto(
        string id,
        string clientOrderId,
        string symbol,
        OrderSide side,
        OrderType type,
        decimal totalQty,
        decimal price,
        decimal levelPrice,
        long createdAt,
        OrderStatus status,
        decimal executedQty,
        decimal executedPrice,
        decimal fee,
        long updatedAt
    )
    {
        Id = id;
        ClientOrderId = clientOrderId;
        Symbol = symbol;
        Side = side;
        Type = type;
        TotalQty = totalQty;
        Price = price;
        LevelPrice = levelPrice;
        CreatedAt = createdAt;
        Status = status;
        ExecutedQty = executedQty;
        ExecutedPrice = executedPrice;
        Fee = fee;
        UpdatedAt = updatedAt;
    }

    public void Update(
        OrderSide side,
        OrderType type,
        decimal totalQty,
        decimal price,
        decimal levelPrice,
        long createdAt,
        OrderStatus status,
        decimal executedQty,
        decimal executedPrice,
        decimal fee,
        long updatedAt
    )
    {
        Side = side;
        Type = type;
        TotalQty = totalQty;
        Price = price;
        LevelPrice = levelPrice;
        CreatedAt = createdAt;
        Status = status;
        ExecutedQty = executedQty;
        ExecutedPrice = executedPrice;
        Fee = fee;
        UpdatedAt = updatedAt;
    }
}
