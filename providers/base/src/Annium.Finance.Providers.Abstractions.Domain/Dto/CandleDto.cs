using NodaTime;

namespace Annium.Finance.Providers.Abstractions.Domain.Dto;

public sealed record CandleDto(Instant Moment, decimal Open, decimal High, decimal Low, decimal Close, decimal Volume);
