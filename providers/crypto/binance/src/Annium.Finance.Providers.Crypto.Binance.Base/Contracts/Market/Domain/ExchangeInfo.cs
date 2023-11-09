using System.Collections.Generic;
using Annium.Finance.Providers.Abstractions.Domain.Dto;

namespace Annium.Finance.Providers.Crypto.Binance.Base.Contracts.Market.Domain;

public sealed record ExchangeInfo(RateLimits RateLimits, IReadOnlyCollection<InstrumentDto> Instruments);
