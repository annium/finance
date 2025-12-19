using Annium.Core.Mapper.Attributes;

namespace Annium.Finance.Providers.Abstractions.Domain.Market.Operations;

[AutoMapped]
public enum MarketOperationStatus
{
    Ok,
    NotConnected,
    NetworkError,
    Aborted,
    TooManyRequests,
    BadRequest,
    NotFound,
    ParseError,
    UnknownError,
}
