using System;

namespace Annium.Finance.Providers.Crypto.Binance.Base.User.Contracts.Domain;

public sealed record CancelOrderResponse(Guid Id, string OrderId);
