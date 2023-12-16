using Annium.Core.Mapper.Attributes;

namespace Annium.Finance.Providers.Abstractions.Domain.Operations;

[AutoMapped]
public enum MarketOperationStatus
{
    NotConnected,
    NetworkError,
    BadRequest,
    NotFound,
    ParseError,
    Ok,
    UnknownError,
}
