namespace Annium.Finance.Providers.Abstractions.Domain.Models;

public sealed record CandleModel(long Moment, decimal Open, decimal High, decimal Low, decimal Close, decimal Volume);
