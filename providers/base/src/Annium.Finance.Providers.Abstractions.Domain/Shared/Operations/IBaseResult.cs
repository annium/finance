using System.Diagnostics.CodeAnalysis;

namespace Annium.Finance.Providers.Abstractions.Domain.Shared.Operations;

/// <summary>
/// Represents the common shape of a provider operation outcome that returns data of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The type of data returned by the operation.</typeparam>
public interface IBaseResult<out T>
{
    /// <summary>Gets a value indicating whether the operation failed because of a network-level error.</summary>
    bool IsNetworkError { get; }

    /// <summary>Gets a value indicating whether the operation was aborted before it could complete.</summary>
    bool IsAborted { get; }

    /// <summary>Gets a value indicating whether the operation completed successfully and <see cref="Data"/> is populated.</summary>
    [MemberNotNullWhen(true, nameof(Data))]
    bool IsSuccess { get; }

    /// <summary>Gets a value indicating whether the operation failed for a reason other than a network error or abort.</summary>
    bool IsFailure { get; }

    /// <summary>Gets the data returned by the operation; set when <see cref="IsSuccess"/> is true.</summary>
    T? Data { get; }

    /// <summary>Gets the message describing the outcome, typically an error detail; empty on success.</summary>
    string Message { get; }
}

/// <summary>
/// Represents the common shape of a provider operation outcome that returns no data.
/// </summary>
public interface IBaseResult
{
    /// <summary>Gets a value indicating whether the operation failed because of a network-level error.</summary>
    bool IsNetworkError { get; }

    /// <summary>Gets a value indicating whether the operation was aborted before it could complete.</summary>
    bool IsAborted { get; }

    /// <summary>Gets a value indicating whether the operation completed successfully.</summary>
    bool IsSuccess { get; }

    /// <summary>Gets a value indicating whether the operation failed for a reason other than a network error or abort.</summary>
    bool IsFailure { get; }

    /// <summary>Gets the message describing the outcome, typically an error detail; empty on success.</summary>
    string Message { get; }
}
