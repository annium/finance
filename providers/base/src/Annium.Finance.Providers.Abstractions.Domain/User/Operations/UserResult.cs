using System.Diagnostics.CodeAnalysis;
using Annium.Finance.Providers.Abstractions.Domain.Shared;
using Annium.Finance.Providers.Abstractions.Domain.Shared.Operations;

namespace Annium.Finance.Providers.Abstractions.Domain.User.Operations;

public sealed record UserResult : IBaseResult
{
    public static UserResult Ok() => new(UserOperationStatus.Ok, string.Empty);

    public static UserResult New(UserOperationStatus status) => new(status, string.Empty);

    public static UserResult New(UserOperationStatus status, string message) => new(status, message);

    public static UserResult From<T>(UserResult<T> result) => new(result.Status, result.Message);

    public static UserResult<T> Ok<T>(T data) => new(UserOperationStatus.Ok, data, string.Empty);

    public static UserResult<T> New<T>(UserOperationStatus status, T data) => new(status, data, string.Empty);

    public static UserResult<T> New<T>(UserOperationStatus status, T data, string error) => new(status, data, error);

    public static UserResult<T> From<T>(UserResult result, T data) => new(result.Status, data, result.Message);

    public static UserResult<T> From<TSource, T>(UserResult<TSource> result, T data) =>
        new(result.Status, data, result.Message);

    public bool IsNetworkError { get; }

    public bool IsSuccess { get; }

    public bool IsAborted { get; }

    public bool IsFailure { get; }

    public UserOperationStatus Status { get; }

    public string Message { get; }

    private UserResult(UserOperationStatus status, string message)
    {
        IsNetworkError = status is UserOperationStatus.NetworkError;
        IsAborted = status is UserOperationStatus.Aborted;
        IsSuccess = status is UserOperationStatus.Ok;
        IsFailure = !IsNetworkError && !IsAborted && !IsSuccess;
        Status = status;
        Message = message;
    }

    public override string ToString() => $"{Status} ({Message})";
}

public sealed record UserResult<T> : IBaseResult<T>
{
    public bool IsNetworkError { get; }

    public bool IsAborted { get; }

    [MemberNotNullWhen(true, nameof(Data))]
    public bool IsSuccess { get; }

    public bool IsFailure { get; }

    public UserOperationStatus Status { get; }

    public T? Data { get; }

    public string Message { get; }

    internal UserResult(UserOperationStatus status, T? data, string message)
    {
        IsNetworkError = status is UserOperationStatus.NetworkError;
        IsAborted = status is UserOperationStatus.Aborted;
        IsSuccess = status is UserOperationStatus.Ok;
        IsFailure = !IsNetworkError && !IsAborted && !IsSuccess;
        Status = status;
        Data = data;
        Message = message;
    }

    public override string ToString() => $"{Status} ({Message})";
}
