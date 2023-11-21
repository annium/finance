namespace Annium.Finance.Providers.Abstractions.Domain.Dto;

public sealed record CandleDto(long Moment, decimal Open, decimal High, decimal Low, decimal Close, decimal Volume);
