using Annium.Core.Mapper.Attributes;

namespace Annium.Finance.Providers.Abstractions.Domain.Operations;

[AutoMapped]
public enum UserOperationStatus
{
    NotConnected,
    NetworkError,
    BadRequest,
    Forbidden,
    NotFound,
    ParseError,
    Ok,
    UnknownError,
}
