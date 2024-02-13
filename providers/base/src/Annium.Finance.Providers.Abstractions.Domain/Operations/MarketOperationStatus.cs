using Annium.Core.Mapper.Attributes;

namespace Annium.Finance.Providers.Abstractions.Domain.Operations;

[AutoMapped]
public enum MarketOperationStatus
{
    Ok,
    NotConnected,
    NetworkError,
    BadRequest,
    NotFound,
    ParseError,
    UnknownError,
}
