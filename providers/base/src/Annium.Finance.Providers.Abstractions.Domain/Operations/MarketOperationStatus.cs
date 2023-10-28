using Annium.Core.Mapper.Attributes;

namespace Annium.Finance.Providers.Abstractions.Domain.Operations;

[AutoMapped]
public enum MarketOperationStatus
{
    NetworkError,
    BadRequest,
    NotFound,
    ParseError,
    Ok,
    UncaughtError,
}
