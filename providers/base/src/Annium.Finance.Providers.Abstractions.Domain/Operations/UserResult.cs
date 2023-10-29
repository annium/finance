using System.Diagnostics.CodeAnalysis;

namespace Annium.Finance.Providers.Abstractions.Domain.Operations;

public sealed record UserResult
{
    public static UserResult Ok() => new(UserOperationStatus.Ok, string.Empty);

    public static UserResult New(UserOperationStatus code) => new(code, string.Empty);

    public static UserResult New(UserOperationStatus code, string message) => new(code, message);

    public static UserResult<T> Ok<T>(T data)
        where T : notnull => new(UserOperationStatus.Ok, data, string.Empty);

    public static UserResult<T> New<T>(UserOperationStatus code, T data)
        where T : notnull => new(code, data, string.Empty);

    public static UserResult<T> New<T>(UserOperationStatus code, T data, string error)
        where T : notnull => new(code, data, error);

    public bool IsSuccess { get; }
    public bool IsFailure { get; }
    public UserOperationStatus Status { get; }
    public string Message { get; }

    private UserResult(UserOperationStatus status, string message)
    {
        IsSuccess = status is UserOperationStatus.Ok;
        IsFailure = !IsSuccess;
        Status = status;
        Message = message;
    }

    public override string ToString() => $"{Status} ({Message})";
}

public sealed record UserResult<T>
{
    [MemberNotNullWhen(true, nameof(Data))]
    public bool IsSuccess { get; }

    [MemberNotNullWhen(false, nameof(Data))]
    public bool IsFailure { get; }
    public UserOperationStatus Status { get; }
    public T? Data { get; }
    public string Message { get; }

    internal UserResult(UserOperationStatus status, T? data, string message)
    {
        IsSuccess = status is UserOperationStatus.Ok;
        IsFailure = !IsSuccess;
        Status = status;
        Data = data;
        Message = message;
    }

    public override string ToString() => $"{Status} ({Message})";
}
