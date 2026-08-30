using System;

namespace Annium.Finance.Providers.Crypto.Binance.Base.User.Contracts.Domain;

/// <summary>Binance's response to a cancel-order request.</summary>
/// <param name="Id">The client-assigned order id (Binance's <c>clientOrderId</c>) of the cancelled order.</param>
/// <param name="OrderId">The Binance-assigned order id (Binance's <c>orderId</c>) of the cancelled order.</param>
public sealed record CancelOrderResponse(Guid Id, string OrderId);
