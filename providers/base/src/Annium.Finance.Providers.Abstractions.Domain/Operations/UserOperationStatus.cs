using Annium.Core.Mapper.Attributes;

namespace Annium.Finance.Providers.Abstractions.Domain.Operations;

[AutoMapped]
public enum UserOperationStatus
{
    Ok,
    NotConnected,
    Aborted,
    NetworkError,
    BadRequest,
    Forbidden,
    NotFound,
    ParseError,
    UnknownError,

    // custom statuses
    InsufficientBalance
}
