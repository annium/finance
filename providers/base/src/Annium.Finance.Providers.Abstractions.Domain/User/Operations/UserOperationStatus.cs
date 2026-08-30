using Annium.Core.Mapper.Attributes;

namespace Annium.Finance.Providers.Abstractions.Domain.User.Operations;

/// <summary>
/// Represents the outcome status of a user (account/trading) provider operation.
/// </summary>
[AutoMapped]
public enum UserOperationStatus
{
    /// <summary>The operation completed successfully.</summary>
    Ok,

    /// <summary>The operation could not be performed because the provider is not connected.</summary>
    NotConnected,

    /// <summary>The operation failed because of a network-level error while communicating with the provider.</summary>
    NetworkError,

    /// <summary>The operation was aborted before it could complete.</summary>
    Aborted,

    /// <summary>The operation was rejected because the provider's rate limit was exceeded.</summary>
    TooManyRequests,

    /// <summary>The operation was rejected because the request was malformed or violated provider constraints.</summary>
    BadRequest,

    /// <summary>The operation was rejected because the account is not authorized to perform it.</summary>
    Forbidden,

    /// <summary>The operation failed because the requested resource (order, position, etc.) was not found.</summary>
    NotFound,

    /// <summary>The operation failed because the provider's response could not be parsed.</summary>
    ParseError,

    /// <summary>The operation failed for a reason not covered by the other statuses.</summary>
    UnknownError,

    // custom statuses
    /// <summary>The operation was rejected because the account does not hold enough free balance to cover it.</summary>
    InsufficientBalance,
}
