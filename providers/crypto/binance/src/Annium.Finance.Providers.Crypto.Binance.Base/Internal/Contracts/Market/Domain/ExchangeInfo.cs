using System.Collections.Generic;
using Annium.Finance.Providers.Abstractions.Domain.Dto;

namespace Annium.Finance.Providers.Crypto.Binance.Base.Internal.Contracts.Market.Domain;

internal readonly record struct ExchangeInfo(RateLimits RateLimits, IReadOnlyCollection<InstrumentDto> Instruments);
